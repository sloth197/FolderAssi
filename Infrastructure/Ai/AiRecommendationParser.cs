using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FolderAssi.Domain.Ai;

namespace FolderAssi.Infrastructure.Ai;

public sealed class AiRecommendationParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex SingleFencePattern = new(
        @"^\s*```(?:json)?\s*(?<json>\{[\s\S]*\})\s*```\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TemplateRecommendationResult Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("AI response is empty.");
        }

        var normalized = NormalizeInput(raw);
        var root = ParseSingleJsonObject(normalized);
        ValidateShape(root);

        var result = JsonSerializer.Deserialize<TemplateRecommendationResult>(
            root.GetRawText(),
            SerializerOptions);

        if (result is null)
        {
            throw new InvalidOperationException("Failed to deserialize AI response JSON.");
        }

        return result;
    }

    private static string NormalizeInput(string raw)
    {
        var trimmed = raw.Trim();
        if (!trimmed.Contains("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var match = SingleFencePattern.Match(trimmed);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "AI response contains markdown code fences or extra text. JSON object only is allowed.");
        }

        return match.Groups["json"].Value.Trim();
    }

    private static JsonElement ParseSingleJsonObject(string json)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidOperationException("AI response must be a JSON object.");
            }

            using var document = JsonDocument.ParseValue(ref reader);

            if (reader.Read())
            {
                throw new InvalidOperationException(
                    "AI response must contain exactly one JSON object without extra content.");
            }

            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("AI response is not valid JSON.", ex);
        }
    }

    private static void ValidateShape(JsonElement root)
    {
        EnsureProperty(root, "templateId", JsonValueKind.String);
        EnsureProperty(root, "variables", JsonValueKind.Object);
        EnsureProperty(root, "options", JsonValueKind.Object);
        EnsureProperty(root, "confidence", JsonValueKind.Number);
        EnsureProperty(root, "notes", JsonValueKind.Array);
    }

    private static void EnsureProperty(JsonElement root, string propertyName, JsonValueKind expectedKind)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"AI response is missing '{propertyName}'.");
        }

        if (property.ValueKind != expectedKind)
        {
            throw new InvalidOperationException(
                $"AI response property '{propertyName}' must be a {expectedKind} value.");
        }
    }
}
