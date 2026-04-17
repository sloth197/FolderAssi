internal static class TemplatePathResolver
{
    public static string ResolveTemplatesPath()
    {
        var candidates = BuildCandidatePaths().ToList();
        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Templates directory was not found. Checked: {string.Join(", ", candidates)}");
    }

    public static IReadOnlyList<string> GetTemplateFiles(string templatesPath)
    {
        if (string.IsNullOrWhiteSpace(templatesPath))
        {
            throw new ArgumentException("Templates path is required.", nameof(templatesPath));
        }

        var normalizedPath = Path.GetFullPath(templatesPath);
        if (!Directory.Exists(normalizedPath))
        {
            throw new DirectoryNotFoundException(
                $"Templates directory was not found: {normalizedPath}");
        }

        return Directory
            .EnumerateFiles(normalizedPath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> BuildCandidatePaths()
    {
        var currentDirectory = Environment.CurrentDirectory;

        // Prefer the runner-owned template source when executed from repository root.
        yield return Path.GetFullPath(Path.Combine(currentDirectory, "FolderAssi.Runner", "templates"));

        // Allow current-directory templates only when current directory is the runner project folder.
        // This prevents accidentally loading repository-root /templates.
        if (IsRunnerProjectDirectory(currentDirectory))
        {
            yield return Path.GetFullPath(Path.Combine(currentDirectory, "templates"));
        }

        // Fallback when current directory is not the executable directory.
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "templates"));
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
}
