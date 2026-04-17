using System.Globalization;
using System.Text.Json;
using FolderAssi.Application.Ai;
using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Templates;
using FolderAssi.Domain.Validation;

namespace FolderAssi.Infrastructure.Ai;

public sealed class AiOutputValidator : IAiOutputValidator
{
    public ValidationResult Validate(
        TemplateRecommendationResult result,
        IReadOnlyCollection<ProjectTemplate> templates)
    {
        var validation = ValidationResult.Success();

        if (result is null)
        {
            validation.AddError("AI_RESULT_NULL", "AI recommendation result is required.");
            return validation;
        }

        if (templates is null || templates.Count == 0)
        {
            validation.AddError("TEMPLATE_COLLECTION_INVALID", "Template collection is required.");
            return validation;
        }

        if (string.IsNullOrWhiteSpace(result.TemplateId))
        {
            validation.AddError("TEMPLATE_ID_MISSING", "templateId is required.", "templateId");
            return validation;
        }

        var template = templates.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Id)
            && string.Equals(t.Id, result.TemplateId, StringComparison.Ordinal));

        if (template is null)
        {
            validation.AddError(
                "TEMPLATE_ID_UNKNOWN",
                $"templateId '{result.TemplateId}' is not an allowed template.",
                "templateId");
            return validation;
        }

        ValidateVariables(result, template, validation);
        ValidateOptions(result, template, validation);
        ValidateConfidence(result, validation);

        return validation;
    }

    private static void ValidateVariables(
        TemplateRecommendationResult result,
        ProjectTemplate template,
        ValidationResult validation)
    {
        var variables = result.Variables ?? new Dictionary<string, string>(StringComparer.Ordinal);

        var allowedKeys = new HashSet<string>(template.RequiredVariables, StringComparer.Ordinal);
        foreach (var key in template.DefaultVariables.Keys)
        {
            allowedKeys.Add(key);
        }

        foreach (var key in variables.Keys)
        {
            if (!allowedKeys.Contains(key))
            {
                validation.AddError(
                    "VARIABLE_KEY_INVALID",
                    $"Variable key '{key}' is not allowed for template '{template.Id}'.",
                    $"variables.{key}");
            }
        }

        foreach (var required in template.RequiredVariables)
        {
            var hasProvided = variables.TryGetValue(required, out var provided)
                && !string.IsNullOrWhiteSpace(provided);
            var hasDefault = template.DefaultVariables.TryGetValue(required, out var defaultValue)
                && !string.IsNullOrWhiteSpace(defaultValue);

            if (!hasProvided && !hasDefault)
            {
                validation.AddError(
                    "VARIABLE_REQUIRED_MISSING",
                    $"Required variable '{required}' is missing.",
                    $"variables.{required}");
            }
        }
    }

    private static void ValidateOptions(
        TemplateRecommendationResult result,
        ProjectTemplate template,
        ValidationResult validation)
    {
        var options = result.Options ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        var allowedOptions = template.Options.ToDictionary(static o => o.Key, StringComparer.Ordinal);

        foreach (var option in options)
        {
            if (!allowedOptions.TryGetValue(option.Key, out var templateOption))
            {
                validation.AddError(
                    "OPTION_KEY_INVALID",
                    $"Option key '{option.Key}' is not defined by template '{template.Id}'.",
                    $"options.{option.Key}");
                continue;
            }

            if (!IsValidOptionValue(option.Value, templateOption.Type))
            {
                validation.AddError(
                    "OPTION_TYPE_INVALID",
                    $"Option '{option.Key}' must match type '{templateOption.Type}'.",
                    $"options.{option.Key}");
            }
        }
    }

    private static void ValidateConfidence(
        TemplateRecommendationResult result,
        ValidationResult validation)
    {
        if (double.IsNaN(result.Confidence) || double.IsInfinity(result.Confidence))
        {
            validation.AddError(
                "CONFIDENCE_INVALID",
                "confidence must be a finite number.",
                "confidence");
            return;
        }

        if (result.Confidence < 0.0d || result.Confidence > 1.0d)
        {
            validation.AddError(
                "CONFIDENCE_OUT_OF_RANGE",
                "confidence must be between 0.0 and 1.0.",
                "confidence");
        }
    }

    private static bool IsValidOptionValue(object? value, string optionType)
    {
        if (string.IsNullOrWhiteSpace(optionType))
        {
            return false;
        }

        return optionType.Trim().ToLowerInvariant() switch
        {
            "string" => IsString(value),
            "enum" => IsString(value),
            "boolean" or "bool" => IsBoolean(value),
            "number" => IsNumeric(value),
            _ => false,
        };
    }

    private static bool IsString(object? value)
    {
        if (value is string)
        {
            return true;
        }

        return value is JsonElement element && element.ValueKind == JsonValueKind.String;
    }

    private static bool IsBoolean(object? value)
    {
        if (value is bool)
        {
            return true;
        }

        return value is JsonElement element
            && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False);
    }

    private static bool IsNumeric(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Number;
        }

        return value switch
        {
            byte or sbyte or short or ushort or int or uint or long or ulong => true,
            float floatValue => !float.IsNaN(floatValue) && !float.IsInfinity(floatValue),
            double doubleValue => !double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue),
            decimal => true,
            _ => double.TryParse(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out _),
        };
    }
}
