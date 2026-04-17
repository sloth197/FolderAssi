using System.Text.RegularExpressions;
using FolderAssi.Application.Templates;

namespace FolderAssi.Infrastructure.Templates;

public sealed class VariableResolver : IVariableResolver
{
    private static readonly Regex PlaceholderPattern = new(
        @"\{\{\s*(?<key>[^{}]+?)\s*\}\}",
        RegexOptions.Compiled);

    private static readonly Regex VariableNamePattern = new(
        @"^[A-Za-z0-9_]+$",
        RegexOptions.Compiled);

    public string Resolve(string input, IReadOnlyDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(variables);

        return PlaceholderPattern.Replace(
            input,
            match =>
            {
                var key = match.Groups["key"].Value.Trim();
                if (!VariableNamePattern.IsMatch(key))
                {
                    throw new InvalidOperationException(
                        $"Variable name '{key}' is invalid. Only letters, numbers, and underscore are allowed.");
                }

                if (!variables.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(
                        $"Variable '{key}' was not provided or is empty.");
                }

                return value;
            });
    }
}
