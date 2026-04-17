using FolderAssi.Application.Ai;
using FolderAssi.Application.Scaffolding;
using FolderAssi.Application.Templates;
using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Templates;
using FolderAssi.Infrastructure.Ai;
using FolderAssi.Infrastructure.Scaffolding;
using FolderAssi.Infrastructure.Templates;

var templatesPath = TemplatePathResolver.ResolveTemplatesPath();

if (args.Contains("--ui", StringComparer.OrdinalIgnoreCase))
{
    await UiWebHost.RunAsync(templatesPath, args);
    return;
}

if (args.Contains("--verify-loader", StringComparer.OrdinalIgnoreCase))
{
    TemplateLoaderVerification.Run(templatesPath);
    return;
}

if (args.Contains("--verify-renderer", StringComparer.OrdinalIgnoreCase))
{
    TemplateRenderingVerification.Run(templatesPath);
    return;
}

if (args.Contains("--verify-scaffold", StringComparer.OrdinalIgnoreCase))
{
    ScaffoldGenerationVerification.Run(templatesPath);
    return;
}

if (args.Contains("--verify-ai", StringComparer.OrdinalIgnoreCase))
{
    AiRecommendationLayerVerification.Run(templatesPath);
    return;
}

var userInput = GetUserInput(args);

ITemplateLoader templateLoader = new JsonTemplateLoader(templatesPath);
ITemplateValidator templateValidator = new TemplateValidator();
IVariableResolver variableResolver = new VariableResolver();
ITemplateRenderer templateRenderer = new TemplateRenderer(variableResolver);
IScaffoldBuilder scaffoldBuilder = new FileSystemScaffoldBuilder();
IArchiveService archiveService = new ZipArchiveService();

ITemplateCandidateFilter candidateFilter = new TemplateCandidateFilter();
IAiTemplateRecommender aiTemplateRecommender = new MockAiTemplateRecommender();
IAiOutputValidator aiOutputValidator = new AiOutputValidator();
ITemplateRecommendationOrchestrator orchestrator = new TemplateRecommendationOrchestrator(
    templateLoader,
    candidateFilter,
    aiTemplateRecommender,
    aiOutputValidator);

Console.WriteLine($"Templates path: {templatesPath}");
Console.WriteLine("Template files:");
foreach (var file in TemplatePathResolver.GetTemplateFiles(templatesPath))
{
    Console.WriteLine($"- {Path.GetFileName(file)}");
}

Console.WriteLine($"User input: {userInput}");

var orchestrationResult = await orchestrator.RecommendAsync(userInput);

Console.WriteLine("Candidate templates:");
foreach (var candidate in orchestrationResult.Candidates)
{
    Console.WriteLine($"- {candidate.Id} ({candidate.Framework})");
}

Console.WriteLine($"Policy: {orchestrationResult.ConfidencePolicy}");

if (orchestrationResult.Recommendation is null)
{
    Console.WriteLine(orchestrationResult.Message);
    return;
}

var recommendation = orchestrationResult.Recommendation;

if (!orchestrationResult.Validation.IsValid)
{
    Console.WriteLine("추천 결과 검증 실패");
    foreach (var error in orchestrationResult.Validation.Errors)
    {
        Console.WriteLine($"- [{error.Code}] {error.Path}: {error.Message}");
    }

    return;
}

switch (orchestrationResult.ConfidencePolicy)
{
    case RecommendationConfidencePolicy.ManualSelectionRequired:
        Console.WriteLine("수동 템플릿 선택이 필요합니다. 자동 생성은 수행하지 않습니다.");
        return;

    case RecommendationConfidencePolicy.UserConfirmationRequired:
        PrintRecommendation(recommendation);
        Console.WriteLine("사용자 확인이 필요합니다. 자동 생성은 수행하지 않습니다.");
        return;

    case RecommendationConfidencePolicy.AutoApproved:
        PrintRecommendation(recommendation);
        Console.WriteLine("추천 결과 검증 통과");
        Console.WriteLine(orchestrationResult.Message);
        break;

    default:
        Console.WriteLine("알 수 없는 추천 정책입니다. 자동 생성은 수행하지 않습니다.");
        return;
}

var template = templateLoader.GetById(recommendation.TemplateId);
var templateValidation = templateValidator.Validate(template);
if (!templateValidation.IsValid)
{
    Console.WriteLine("Template validation failed.");
    foreach (var error in templateValidation.Errors)
    {
        Console.WriteLine($"- [{error.Code}] {error.Path}: {error.Message}");
    }

    return;
}

var renderOptions = ConvertRenderOptions(recommendation.Options);
var renderedRoot = templateRenderer.Render(template, recommendation.Variables, renderOptions);

Console.WriteLine("Rendered tree preview:");
PrintTree(renderedRoot, 0);

var generatedRootPath = Path.Combine(Environment.CurrentDirectory, "generated");
var archivesRootPath = Path.Combine(Environment.CurrentDirectory, "archives");

Directory.CreateDirectory(generatedRootPath);
Directory.CreateDirectory(archivesRootPath);

var generatedProjectPath = scaffoldBuilder.BuildProject(generatedRootPath, renderedRoot);
var generatedZipPath = archiveService.CreateZip(
    generatedProjectPath,
    Path.Combine(archivesRootPath, $"{renderedRoot.Name}.zip"));

Console.WriteLine($"Generated project path: {generatedProjectPath}");
Console.WriteLine($"Generated zip path: {generatedZipPath}");

static string GetUserInput(string[] args)
{
    const string defaultInput = "JWT 로그인 기능이 포함된 ASP.NET Core Web API 프로젝트를 만들고 싶어";
    var inputArgument = args.FirstOrDefault(static arg =>
        arg.StartsWith("--input=", StringComparison.Ordinal));

    if (string.IsNullOrWhiteSpace(inputArgument))
    {
        return defaultInput;
    }

    return inputArgument["--input=".Length..].Trim();
}

static Dictionary<string, object?> ConvertRenderOptions(IReadOnlyDictionary<string, object?> options)
{
    var result = new Dictionary<string, object?>(StringComparer.Ordinal);

    foreach (var option in options)
    {
        result[option.Key] = option.Value
            ?? throw new InvalidOperationException(
                $"Option '{option.Key}' must not be null for rendering.");
    }

    return result;
}

static void PrintRecommendation(TemplateRecommendationResult recommendation)
{
    Console.WriteLine($"Recommended templateId: {recommendation.TemplateId}");
    Console.WriteLine($"Confidence: {recommendation.Confidence:0.00}");
    Console.WriteLine("Variables:");
    PrintDictionary(recommendation.Variables);
    Console.WriteLine("Options:");
    PrintDictionary(recommendation.Options);
    Console.WriteLine("Notes:");
    if (recommendation.Notes.Count == 0)
    {
        Console.WriteLine("- (none)");
        return;
    }

    foreach (var note in recommendation.Notes)
    {
        Console.WriteLine($"- {note}");
    }
}

static void PrintDictionary<T>(IReadOnlyDictionary<string, T> values)
{
    if (values.Count == 0)
    {
        Console.WriteLine("- (none)");
        return;
    }

    foreach (var item in values)
    {
        Console.WriteLine($"- {item.Key}: {item.Value}");
    }
}

static void PrintTree(TemplateNode node, int depth)
{
    var indent = new string(' ', depth * 2);
    var marker = node.Type == TemplateNodeType.Folder ? "[D]" : "[F]";
    Console.WriteLine($"{indent}{marker} {node.Name}");

    foreach (var child in node.Children)
    {
        PrintTree(child, depth + 1);
    }
}
