using System.Text;
using FolderAssi.Application.Templates;
using FolderAssi.Infrastructure.Templates;

internal static class TemplateLoaderVerification
{
    public static void Run(string templatesPath)
    {
        Console.WriteLine("== JsonTemplateLoader Verification ==");
        VerifyMissingTemplatesFolder();
        VerifyInvalidJsonFile();
        VerifyNormalLoading(templatesPath);
        VerifyUnknownTemplateId(templatesPath);
    }

    private static void VerifyMissingTemplatesFolder()
    {
        Console.WriteLine("[1] Missing templates folder");

        var tempRoot = CreateTempRoot();
        var missingPath = Path.Combine(tempRoot, "templates");

        try
        {
            ITemplateLoader loader = new JsonTemplateLoader(missingPath);
            _ = loader.LoadAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    private static void VerifyInvalidJsonFile()
    {
        Console.WriteLine("[2] Invalid JSON file");

        var tempRoot = CreateTempRoot();
        var templatesPath = Path.Combine(tempRoot, "templates");
        Directory.CreateDirectory(templatesPath);

        var invalidFilePath = Path.Combine(templatesPath, "broken-template.json");
        File.WriteAllText(
            invalidFilePath,
            """
            {
              "id": "broken-template",
              "name": "Broken Template",
              "root": {
                "name": "broken-root",
                "type": "folder"
              }
            """,
            Encoding.UTF8);

        try
        {
            ITemplateLoader loader = new JsonTemplateLoader(templatesPath);
            _ = loader.LoadAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    private static void VerifyNormalLoading(string templatesPath)
    {
        Console.WriteLine("[3] Normal loading");

        ITemplateLoader loader = new JsonTemplateLoader(templatesPath);
        var templates = loader.LoadAll();

        Console.WriteLine($"Resolved templates path: {templatesPath}");
        Console.WriteLine("Template files:");
        foreach (var file in TemplatePathResolver.GetTemplateFiles(templatesPath))
        {
            Console.WriteLine($"- {Path.GetFileName(file)}");
        }

        Console.WriteLine($"Loaded templates: {templates.Count}");
        foreach (var template in templates)
        {
            Console.WriteLine($"- {template.Id}");
        }
    }

    private static void VerifyUnknownTemplateId(string templatesPath)
    {
        Console.WriteLine("[4] Unknown templateId");

        ITemplateLoader loader = new JsonTemplateLoader(templatesPath);

        try
        {
            _ = loader.GetById("does-not-exist");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
            Console.WriteLine(ex.Message);
        }
    }

    private static string CreateTempRoot()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FolderAssi",
            "JsonTemplateLoader",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }

    private static void Cleanup(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
