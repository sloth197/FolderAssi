using System.Text.Json;
using FolderAssi.Application.Ai;
using FolderAssi.Application.Scaffolding;
using FolderAssi.Application.Templates;
using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Templates;
using FolderAssi.Infrastructure.Ai;
using FolderAssi.Infrastructure.Scaffolding;
using FolderAssi.Infrastructure.Templates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

internal static class UiWebHost
{
    public static async Task RunAsync(string defaultTemplatesPath, string[] args)
    {
        var filteredArgs = args
            .Where(static arg => !string.Equals(arg, "--ui", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var builder = WebApplication.CreateBuilder(filteredArgs);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });

        var app = builder.Build();

        var settingsFilePath = ResolveSettingsFilePath();
        ISettingsService settingsService = new FileSettingsService(settingsFilePath, defaultTemplatesPath);
        var historyFilePath = ResolveHistoryFilePath();
        IHistoryService historyService = new FileHistoryService(historyFilePath);
        using var sharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        var uiRootCandidates = BuildUiRootCandidates().ToList();
        var uiRoot = uiRootCandidates.FirstOrDefault(IsValidUiRoot)
            ?? uiRootCandidates.FirstOrDefault()
            ?? string.Empty;
        var uiRootExists = IsValidUiRoot(uiRoot);
        if (uiRootExists)
        {
            var provider = new PhysicalFileProvider(uiRoot);

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = provider
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = provider
            });
        }

        app.MapGet("/api/health", () =>
        {
            try
            {
                var settingsView = settingsService.GetView();
                return Results.Ok(new
                {
                    status = "ok",
                    settingsFilePath = settingsService.SettingsFilePath,
                    historyFilePath = historyService.HistoryFilePath,
                    settings = settingsView,
                    uiRoot,
                    uiRootExists,
                    uiRootCandidates
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    status = "error",
                    message = ex.Message,
                    settingsFilePath = settingsService.SettingsFilePath
                });
            }
        });

        app.MapGet("/api/settings", () =>
        {
            try
            {
                return Results.Ok(settingsService.GetView());
            }
            catch (SettingsValidationException ex)
            {
                return Results.BadRequest(new
                {
                    message = ex.Message,
                    errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        app.MapPost("/api/settings", (AppSettingsUpdateRequest request) =>
        {
            try
            {
                settingsService.Save(request);
                return Results.Ok(new
                {
                    message = "Settings saved.",
                    settings = settingsService.GetView()
                });
            }
            catch (SettingsValidationException ex)
            {
                return Results.BadRequest(new
                {
                    message = ex.Message,
                    errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        app.MapGet("/api/history", (int? limit) =>
        {
            try
            {
                var normalizedLimit = Math.Clamp(limit ?? 20, 1, 100);
                var items = historyService.GetRecent(normalizedLimit);
                return Results.Ok(new
                {
                    historyFilePath = historyService.HistoryFilePath,
                    items
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        app.MapGet("/api/templates", () =>
        {
            if (!TryBuildRuntime(settingsService, sharedHttpClient, out var runtime, out var errorResult))
            {
                return errorResult!;
            }

            var templates = runtime.TemplateLoader.LoadAll()
                .Select(static template => new UiTemplateDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    Language = template.Language,
                    Framework = template.Framework,
                    Tags = template.Tags.ToList(),
                    RequiredVariables = template.RequiredVariables.ToList(),
                    DefaultVariables = new Dictionary<string, string>(template.DefaultVariables, StringComparer.Ordinal),
                    Options = template.Options
                        .Select(static option => new UiTemplateOptionDto
                        {
                            Key = option.Key,
                            Label = option.Label,
                            Type = option.Type,
                            Default = option.Default
                        })
                        .ToList()
                })
                .OrderBy(static template => template.Id, StringComparer.Ordinal)
                .ToList();

            return Results.Ok(templates);
        });

        app.MapPost("/api/recommend", async (UiRecommendRequest request, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserInput))
            {
                return Results.BadRequest(new { message = "userInput is required." });
            }

            if (!TryBuildRuntime(settingsService, sharedHttpClient, out var runtime, out var errorResult))
            {
                return errorResult!;
            }

            try
            {
                var result = await runtime.Orchestrator.RecommendAsync(request.UserInput.Trim(), cancellationToken);
                return Results.Ok(new
                {
                    candidates = result.Candidates,
                    recommendation = result.Recommendation,
                    validation = result.Validation,
                    policy = result.ConfidencePolicy.ToString(),
                    message = result.Message,
                    canProceed = result.CanProceed
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        app.MapPost("/api/preview", (UiScaffoldRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.TemplateId))
            {
                return Results.BadRequest(new { message = "templateId is required." });
            }

            if (!TryBuildRuntime(settingsService, sharedHttpClient, out var runtime, out var errorResult))
            {
                return errorResult!;
            }

            ProjectTemplate template;
            try
            {
                template = runtime.TemplateLoader.GetById(request.TemplateId.Trim());
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            var validation = runtime.TemplateValidator.Validate(template);
            if (!validation.IsValid)
            {
                return Results.BadRequest(new
                {
                    message = "Template validation failed.",
                    errors = validation.Errors
                });
            }

            var variables = CloneVariables(request.Variables);
            var options = NormalizeRenderOptions(request.Options);

            TemplateNode renderedRoot;
            try
            {
                renderedRoot = runtime.TemplateRenderer.Render(template, variables, options);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException or ArgumentException)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            return Results.Ok(new
            {
                tree = MapTree(renderedRoot)
            });
        });

        app.MapPost("/api/generate", (UiScaffoldRequest request) =>
        {
            var userInput = request.UserInput?.Trim() ?? string.Empty;
            var selectedTemplateId = request.TemplateId?.Trim() ?? string.Empty;
            var variables = CloneVariables(request.Variables);
            var options = CloneOptions(request.Options);

            if (string.IsNullOrWhiteSpace(selectedTemplateId))
            {
                TryRecordHistory(
                    historyService,
                    CreateHistoryEntry(
                        userInput,
                        selectedTemplateId,
                        variables,
                        options,
                        success: false,
                        generatedProjectPath: string.Empty,
                        generatedZipPath: string.Empty,
                        failureReason: "templateId is required."));

                return Results.BadRequest(new { message = "templateId is required." });
            }

            if (!TryBuildRuntime(settingsService, sharedHttpClient, out var runtime, out var errorResult))
            {
                TryRecordHistory(
                    historyService,
                    CreateHistoryEntry(
                        userInput,
                        selectedTemplateId,
                        variables,
                        options,
                        success: false,
                        generatedProjectPath: string.Empty,
                        generatedZipPath: string.Empty,
                        failureReason: "Runtime service initialization failed."));

                return errorResult!;
            }

            ProjectTemplate template;
            try
            {
                template = runtime.TemplateLoader.GetById(selectedTemplateId);
            }
            catch (InvalidOperationException ex)
            {
                TryRecordHistory(
                    historyService,
                    CreateHistoryEntry(
                        userInput,
                        selectedTemplateId,
                        variables,
                        options,
                        success: false,
                        generatedProjectPath: string.Empty,
                        generatedZipPath: string.Empty,
                        failureReason: ex.Message));

                return Results.BadRequest(new { message = ex.Message });
            }

            var validation = runtime.TemplateValidator.Validate(template);
            if (!validation.IsValid)
            {
                TryRecordHistory(
                    historyService,
                    CreateHistoryEntry(
                        userInput,
                        selectedTemplateId,
                        variables,
                        options,
                        success: false,
                        generatedProjectPath: string.Empty,
                        generatedZipPath: string.Empty,
                        failureReason: "Template validation failed."));

                return Results.BadRequest(new
                {
                    message = "Template validation failed.",
                    errors = validation.Errors
                });
            }

            var renderOptions = NormalizeRenderOptions(options);

            TemplateNode renderedRoot;
            try
            {
                renderedRoot = runtime.TemplateRenderer.Render(template, variables, renderOptions);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException or ArgumentException)
            {
                TryRecordHistory(
                    historyService,
                    CreateHistoryEntry(
                        userInput,
                        selectedTemplateId,
                        variables,
                        options,
                        success: false,
                        generatedProjectPath: string.Empty,
                        generatedZipPath: string.Empty,
                        failureReason: ex.Message));

                return Results.BadRequest(new { message = ex.Message });
            }

            Directory.CreateDirectory(runtime.Settings.OutputPath);
            Directory.CreateDirectory(runtime.Settings.ZipPath);

            string generatedProjectPath;
            string generatedZipPath;
            try
            {
                generatedProjectPath = runtime.ScaffoldBuilder.BuildProject(runtime.Settings.OutputPath, renderedRoot);
                generatedZipPath = runtime.ArchiveService.CreateZip(
                    generatedProjectPath,
                    Path.Combine(runtime.Settings.ZipPath, $"{renderedRoot.Name}.zip"));
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
            {
                TryRecordHistory(
                    historyService,
                    CreateHistoryEntry(
                        userInput,
                        selectedTemplateId,
                        variables,
                        options,
                        success: false,
                        generatedProjectPath: string.Empty,
                        generatedZipPath: string.Empty,
                        failureReason: ex.Message));

                return Results.BadRequest(new { message = ex.Message });
            }

            TryRecordHistory(
                historyService,
                CreateHistoryEntry(
                    userInput,
                    selectedTemplateId,
                    variables,
                    options,
                    success: true,
                    generatedProjectPath,
                    generatedZipPath,
                    failureReason: string.Empty));

            return Results.Ok(new
            {
                generatedProjectPath,
                generatedZipPath,
                tree = MapTree(renderedRoot)
            });
        });

        app.MapFallback(async context =>
        {
            if (!uiRootExists)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "UI root was not found.",
                    uiRoot,
                    uiRootCandidates
                });
                return;
            }

            var indexPath = Path.Combine(uiRoot, "index.html");
            if (!File.Exists(indexPath))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "index.html was not found.",
                    indexPath
                });
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexPath);
        });

        Console.WriteLine("FolderAssi UI host is running.");
        Console.WriteLine($"Settings file: {settingsService.SettingsFilePath}");
        Console.WriteLine($"UI root: {uiRoot}");
        Console.WriteLine("Open http://localhost:5078");

        await app.RunAsync("http://localhost:5078");
    }

    private static bool TryBuildRuntime(
        ISettingsService settingsService,
        HttpClient sharedHttpClient,
        out RuntimeServices runtime,
        out IResult? errorResult)
    {
        try
        {
            var settings = settingsService.Get();
            ITemplateLoader templateLoader = new JsonTemplateLoader(settings.TemplatesPath);
            ITemplateValidator templateValidator = new TemplateValidator();
            IVariableResolver variableResolver = new VariableResolver();
            ITemplateRenderer templateRenderer = new TemplateRenderer(variableResolver);
            IScaffoldBuilder scaffoldBuilder = new FileSystemScaffoldBuilder();
            IArchiveService archiveService = new ZipArchiveService();

            IAiTemplateRecommender aiTemplateRecommender = settings.AiMode == AiMode.Real
                ? new OpenAiTemplateRecommender(
                    sharedHttpClient,
                    new OpenAiRecommendationOptions
                    {
                        ApiKey = ResolveEffectiveApiKey(settings),
                        Endpoint = settings.OpenAiEndpoint,
                        Model = settings.Model
                    },
                    new AiPromptBuilder(),
                    new AiRecommendationParser())
                : new MockAiTemplateRecommender();

            IAiOutputValidator aiOutputValidator = new AiOutputValidator();
            ITemplateCandidateFilter candidateFilter = new TemplateCandidateFilter();
            ITemplateRecommendationOrchestrator orchestrator = new TemplateRecommendationOrchestrator(
                templateLoader,
                candidateFilter,
                aiTemplateRecommender,
                aiOutputValidator);

            runtime = new RuntimeServices
            {
                Settings = settings,
                TemplateLoader = templateLoader,
                TemplateValidator = templateValidator,
                TemplateRenderer = templateRenderer,
                ScaffoldBuilder = scaffoldBuilder,
                ArchiveService = archiveService,
                Orchestrator = orchestrator
            };

            errorResult = null;
            return true;
        }
        catch (SettingsValidationException ex)
        {
            runtime = RuntimeServices.Empty;
            errorResult = Results.BadRequest(new
            {
                message = ex.Message,
                errors = ex.Errors
            });
            return false;
        }
        catch (Exception ex)
        {
            runtime = RuntimeServices.Empty;
            errorResult = Results.BadRequest(new { message = ex.Message });
            return false;
        }
    }

    private static string ResolveSettingsFilePath()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FolderAssi");

        return Path.Combine(settingsDirectory, "settings.json");
    }

    private static string ResolveHistoryFilePath()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FolderAssi");

        return Path.Combine(settingsDirectory, "generation-history.json");
    }

    private static void TryRecordHistory(IHistoryService historyService, GenerationHistoryEntry entry)
    {
        try
        {
            historyService.Add(entry);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[History] failed to save entry: {ex.Message}");
        }
    }

    private static GenerationHistoryEntry CreateHistoryEntry(
        string userInput,
        string selectedTemplateId,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, object?> options,
        bool success,
        string generatedProjectPath,
        string generatedZipPath,
        string failureReason)
    {
        return new GenerationHistoryEntry
        {
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UserInput = userInput ?? string.Empty,
            SelectedTemplateId = selectedTemplateId ?? string.Empty,
            Variables = new Dictionary<string, string>(variables, StringComparer.Ordinal),
            Options = new Dictionary<string, object?>(options, StringComparer.Ordinal),
            GeneratedProjectPath = generatedProjectPath ?? string.Empty,
            GeneratedZipPath = generatedZipPath ?? string.Empty,
            Success = success,
            FailureReason = failureReason ?? string.Empty
        };
    }

    private static Dictionary<string, string> CloneVariables(
        IReadOnlyDictionary<string, string>? variables)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (variables is null)
        {
            return result;
        }

        foreach (var pair in variables)
        {
            result[pair.Key] = pair.Value ?? string.Empty;
        }

        return result;
    }

    private static Dictionary<string, object?> CloneOptions(
        IReadOnlyDictionary<string, object?>? options)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (options is null)
        {
            return result;
        }

        foreach (var pair in options)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static Dictionary<string, object?> NormalizeRenderOptions(
        IReadOnlyDictionary<string, object?>? options)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (options is null)
        {
            return result;
        }

        foreach (var pair in options)
        {
            var normalized = NormalizeOptionValue(pair.Value);
            if (normalized is not null)
            {
                result[pair.Key] = normalized;
            }
        }

        return result;
    }

    private static object? NormalizeOptionValue(object? value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var intValue)
                    ? intValue
                    : element.TryGetDouble(out var doubleValue)
                        ? doubleValue
                        : element.GetRawText(),
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        return value;
    }

    private static IEnumerable<string> BuildUiRootCandidates()
    {
        var currentDirectory = Environment.CurrentDirectory;

        // Use currentDirectory/ui only when current directory is runner project folder.
        if (IsRunnerProjectDirectory(currentDirectory))
        {
            yield return Path.GetFullPath(Path.Combine(currentDirectory, "ui"));
        }

        // Prefer repository-root execution path -> FolderAssi.Runner/ui
        yield return Path.GetFullPath(Path.Combine(currentDirectory, "FolderAssi.Runner", "ui"));

        // Fallback for bin execution
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "ui"));
    }

    private static bool IsValidUiRoot(string candidate)
    {
        if (!Directory.Exists(candidate))
        {
            return false;
        }

        var indexPath = Path.Combine(candidate, "index.html");
        var appJsPath = Path.Combine(candidate, "app.js");

        if (!File.Exists(indexPath) || !File.Exists(appJsPath))
        {
            return false;
        }

        var indexInfo = new FileInfo(indexPath);
        return indexInfo.Length > 0;
    }

    private static bool IsRunnerProjectDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return false;
        }

        var projectFilePath = Path.Combine(directoryPath, "FolderAssi.Runner.csproj");
        return File.Exists(projectFilePath);
    }

    private static UiTreeNodeDto MapTree(TemplateNode node)
    {
        return new UiTreeNodeDto
        {
            Name = node.Name,
            Type = node.Type.ToString(),
            Children = node.Children.Select(MapTree).ToList()
        };
    }

    private static string ResolveEffectiveApiKey(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            return settings.OpenAiApiKey;
        }

        var openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (!string.IsNullOrWhiteSpace(openRouterApiKey))
        {
            return openRouterApiKey.Trim();
        }

        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(openAiApiKey))
        {
            return openAiApiKey.Trim();
        }

        return string.Empty;
    }
}

internal sealed record class RuntimeServices
{
    public static RuntimeServices Empty { get; } = new();

    public AppSettings Settings { get; init; } = new();
    public ITemplateLoader TemplateLoader { get; init; } = null!;
    public ITemplateValidator TemplateValidator { get; init; } = null!;
    public ITemplateRenderer TemplateRenderer { get; init; } = null!;
    public IScaffoldBuilder ScaffoldBuilder { get; init; } = null!;
    public IArchiveService ArchiveService { get; init; } = null!;
    public ITemplateRecommendationOrchestrator Orchestrator { get; init; } = null!;
}

internal sealed record class UiRecommendRequest
{
    public string UserInput { get; init; } = string.Empty;
}

internal sealed record class UiScaffoldRequest
{
    public string UserInput { get; init; } = string.Empty;
    public string TemplateId { get; init; } = string.Empty;
    public Dictionary<string, string> Variables { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, object?> Options { get; init; } = new(StringComparer.Ordinal);
}

internal sealed record class UiTemplateOptionDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public object? Default { get; init; }
}

internal sealed record class UiTemplateDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Framework { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public List<string> RequiredVariables { get; init; } = [];
    public Dictionary<string, string> DefaultVariables { get; init; } = new(StringComparer.Ordinal);
    public List<UiTemplateOptionDto> Options { get; init; } = [];
}

internal sealed record class UiTreeNodeDto
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public List<UiTreeNodeDto> Children { get; init; } = [];
}
