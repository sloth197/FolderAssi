using FolderAssi.Application.Scaffolding;
using FolderAssi.Application.Templates;
using FolderAssi.Infrastructure.Scaffolding;
using FolderAssi.Infrastructure.Templates;

internal static class ScaffoldGenerationVerification
{
    public static void Run(string templatesPath)
    {
        Console.WriteLine("== Scaffold Generation Verification ==");

        ITemplateLoader templateLoader = new JsonTemplateLoader(templatesPath);
        ITemplateValidator templateValidator = new TemplateValidator();
        IVariableResolver variableResolver = new VariableResolver();
        ITemplateRenderer templateRenderer = new TemplateRenderer(variableResolver);
        IScaffoldBuilder scaffoldBuilder = new FileSystemScaffoldBuilder();
        IArchiveService archiveService = new ZipArchiveService();

        var template = templateLoader.GetById("aspnetcore-webapi-starter");
        var validationResult = templateValidator.Validate(template);

        if (!validationResult.IsValid)
        {
            Console.WriteLine("Template validation failed.");
            foreach (var error in validationResult.Errors)
            {
                Console.WriteLine($"- [{error.Code}] {error.Path}: {error.Message}");
            }

            return;
        }

        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi",
        };

        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true,
        };

        var renderedRoot = templateRenderer.Render(template, variables, options);

        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FolderAssi",
            "ScaffoldVerification",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);

            var generatedProjectPath = scaffoldBuilder.BuildProject(tempRoot, renderedRoot);
            var generatedZipPath = archiveService.CreateZip(
                generatedProjectPath,
                Path.Combine(tempRoot, "archives", $"{renderedRoot.Name}.zip"));

            PrintCheck("Project root folder", Directory.Exists(generatedProjectPath));
            PrintCheck("Controllers folder", Directory.Exists(Path.Combine(generatedProjectPath, "Controllers")));
            PrintCheck("Program.cs file", File.Exists(Path.Combine(generatedProjectPath, "Program.cs")));
            PrintCheck("Auth folder when includeAuth=true", Directory.Exists(Path.Combine(generatedProjectPath, "Auth")));
            PrintCheck("ZIP file", File.Exists(generatedZipPath));

            Console.WriteLine($"Generated project path: {generatedProjectPath}");
            Console.WriteLine($"Generated zip path: {generatedZipPath}");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void PrintCheck(string label, bool passed)
    {
        Console.WriteLine($"{label}: {(passed ? "PASS" : "FAIL")}");
    }
}
