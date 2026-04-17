using System.Text.Json;
using System.Text.Json.Serialization;
using FolderAssi.Application.Templates;
using FolderAssi.Domain.Templates;

namespace FolderAssi.Infrastructure.Templates;

public sealed class JsonTemplateLoader : ITemplateLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly string _templatesDirectoryPath;
    private IReadOnlyList<ProjectTemplate>? _cachedTemplates;
    private Dictionary<string, ProjectTemplate>? _templateById;

    public JsonTemplateLoader(string templatesDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(templatesDirectoryPath))
        {
            throw new ArgumentException("Templates directory path is required.", nameof(templatesDirectoryPath));
        }

        _templatesDirectoryPath = Path.GetFullPath(templatesDirectoryPath);
    }

    public IReadOnlyList<ProjectTemplate> LoadAll()
    {
        EnsureCacheLoaded();
        return _cachedTemplates!;
    }

    public ProjectTemplate GetById(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new InvalidOperationException("templateId is required.");
        }

        EnsureCacheLoaded();

        if (_templateById!.TryGetValue(templateId, out var template))
        {
            return template;
        }

        throw new InvalidOperationException(
            $"Template with id '{templateId}' was not found in '{_templatesDirectoryPath}'.");
    }

    private void EnsureCacheLoaded()
    {
        if (_cachedTemplates is not null && _templateById is not null)
        {
            return;
        }

        if (!Directory.Exists(_templatesDirectoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Templates directory was not found: {_templatesDirectoryPath}");
        }

        var templates = Directory
            .EnumerateFiles(_templatesDirectoryPath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToList();

        var templateById = new Dictionary<string, ProjectTemplate>(StringComparer.Ordinal);

        foreach (var template in templates)
        {
            if (string.IsNullOrWhiteSpace(template.Id))
            {
                throw new InvalidDataException(
                    "A template was loaded without an id. JsonTemplateLoader requires every template to define a non-empty id.");
            }

            if (!templateById.TryAdd(template.Id, template))
            {
                throw new InvalidDataException(
                    $"Duplicate template id '{template.Id}' was found in '{_templatesDirectoryPath}'.");
            }
        }

        _cachedTemplates = templates;
        _templateById = templateById;
    }

    private static ProjectTemplate LoadFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var template = JsonSerializer.Deserialize<ProjectTemplate>(json, SerializerOptions);

            if (template is null)
            {
                throw new InvalidDataException($"Template file '{filePath}' could not be deserialized.");
            }

            return template;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Template file '{filePath}' contains invalid JSON.",
                ex);
        }
    }
}
