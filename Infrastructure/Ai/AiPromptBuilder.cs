using System.Text.Json;
using System.Text;
using FolderAssi.Domain.Ai;

namespace FolderAssi.Infrastructure.Ai;

public sealed class AiPromptBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string BuildSystemPrompt()
    {
        return """
You are FolderAssi's template recommender.

You must follow these rules:
1. Never invent a templateId.
2. Never select a templateId outside the provided candidates.
3. Never create option keys that are not defined by the selected candidate.
4. Only include variables that are needed.
5. Never generate folder structure, file structure, file path, or source code.
6. Output JSON only.
7. Do not use markdown.
8. Do not use fenced code blocks.

Output must be exactly one JSON object with this shape:
{
  "templateId": "string",
  "variables": {},
  "options": {},
  "confidence": 0.0,
  "notes": []
}
""";
    }

    public string BuildUserPrompt(TemplateRecommendationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            throw new ArgumentException("UserInput is required.", nameof(request));
        }

        if (request.Candidates.Count == 0)
        {
            throw new ArgumentException("At least one template candidate is required.", nameof(request));
        }

        var candidatesJson = JsonSerializer.Serialize(request.Candidates, JsonOptions);
        var outputSchema = """
{
  "templateId": "string",
  "variables": {},
  "options": {},
  "confidence": 0.0,
  "notes": []
}
""";

        var builder = new StringBuilder();
        builder.AppendLine("User request:");
        builder.AppendLine($"\"{request.UserInput.Trim()}\"");
        builder.AppendLine();
        builder.AppendLine("Template candidates (choose only from this list):");
        builder.AppendLine(candidatesJson);
        builder.AppendLine();
        builder.AppendLine("Output schema:");
        builder.AppendLine(outputSchema);
        builder.AppendLine();
        builder.AppendLine("Reminder:");
        builder.AppendLine("- Choose exactly one templateId from candidates.");
        builder.AppendLine("- Do not invent templateId, option keys, or variable keys outside candidate scope.");
        builder.AppendLine("- Do not generate folder/file structures.");
        builder.AppendLine("- Return only one JSON object.");

        return builder.ToString().TrimEnd();
    }
}
