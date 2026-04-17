using System.Text.Json;
using System.Text.Json.Serialization;
using FolderAssi.Infrastructure.Templates;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Templates;

public sealed class JsonTemplateLoaderTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    [Fact]
    public void LoadAll_WithValidJsonFiles_ReturnsTemplates()
    {
        using var temp = new TemporaryDirectory();
        var templatesPath = temp.CreateSubdirectory("templates");

        WriteTemplate(templatesPath, TestTemplateFactory.CreateAspNetTemplate());
        WriteTemplate(templatesPath, TestTemplateFactory.CreateSpringTemplate());

        var loader = new JsonTemplateLoader(templatesPath);

        var all = loader.LoadAll();
        var asp = loader.GetById("aspnetcore-webapi-starter");

        Assert.Equal(2, all.Count);
        Assert.Equal("aspnetcore-webapi-starter", asp.Id);
        Assert.Contains(all, t => t.Id == "spring-boot-layered-api-starter");
    }

    [Fact]
    public void LoadAll_WhenTemplateDirectoryMissing_ThrowsDirectoryNotFoundException()
    {
        using var temp = new TemporaryDirectory();
        var missingPath = temp.GetPath("missing-templates");
        var loader = new JsonTemplateLoader(missingPath);

        var ex = Assert.Throws<DirectoryNotFoundException>(() => loader.LoadAll());

        Assert.Contains("Templates directory was not found", ex.Message);
    }

    [Fact]
    public void LoadAll_WhenJsonIsInvalid_ThrowsInvalidDataExceptionWithFileName()
    {
        using var temp = new TemporaryDirectory();
        var templatesPath = temp.CreateSubdirectory("templates");
        var brokenFile = Path.Combine(templatesPath, "broken-template.json");
        File.WriteAllText(brokenFile, "{ \"id\": \"x\", ");

        var loader = new JsonTemplateLoader(templatesPath);

        var ex = Assert.Throws<InvalidDataException>(() => loader.LoadAll());

        Assert.Contains("contains invalid JSON", ex.Message);
        Assert.Contains("broken-template.json", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetById_WhenTemplateIdMissing_ThrowsInvalidOperationException()
    {
        using var temp = new TemporaryDirectory();
        var templatesPath = temp.CreateSubdirectory("templates");
        WriteTemplate(templatesPath, TestTemplateFactory.CreateAspNetTemplate());

        var loader = new JsonTemplateLoader(templatesPath);
        loader.LoadAll();

        var ex = Assert.Throws<InvalidOperationException>(() => loader.GetById("unknown-template"));

        Assert.Contains("Template with id 'unknown-template' was not found", ex.Message);
    }

    private static void WriteTemplate(string templatesPath, object template)
    {
        var json = JsonSerializer.Serialize(template, SerializerOptions);
        var id = (string)template.GetType().GetProperty("Id")!.GetValue(template)!;
        File.WriteAllText(Path.Combine(templatesPath, $"{id}.json"), json);
    }
}
