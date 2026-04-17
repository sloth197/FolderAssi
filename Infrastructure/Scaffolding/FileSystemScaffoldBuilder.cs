using System.Text;
using FolderAssi.Application.Scaffolding;
using FolderAssi.Domain.Templates;

namespace FolderAssi.Infrastructure.Scaffolding;

public sealed class FileSystemScaffoldBuilder : IScaffoldBuilder
{
    public string BuildProject(string outputRootPath, TemplateNode renderedRoot)
    {
        if (string.IsNullOrWhiteSpace(outputRootPath))
        {
            throw new ArgumentException("Output root path is required.", nameof(outputRootPath));
        }

        ArgumentNullException.ThrowIfNull(renderedRoot);

        if (renderedRoot.Type != TemplateNodeType.Folder)
        {
            throw new InvalidOperationException("Rendered root node must be a folder.");
        }

        ValidateNodeName(renderedRoot.Name);

        var normalizedOutputRootPath = Path.GetFullPath(outputRootPath);
        Directory.CreateDirectory(normalizedOutputRootPath);

        var projectRootPath = Path.Combine(normalizedOutputRootPath, renderedRoot.Name);
        CreateFolderNode(projectRootPath, renderedRoot);

        return projectRootPath;
    }

    private void CreateNode(string path, TemplateNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ValidateNodeName(node.Name);

        switch (node.Type)
        {
            case TemplateNodeType.Folder:
                CreateFolderNode(path, node);
                break;

            case TemplateNodeType.File:
                CreateFileNode(path, node);
                break;

            default:
                throw new InvalidOperationException($"Unsupported template node type '{node.Type}'.");
        }
    }

    private void CreateFolderNode(string folderPath, TemplateNode node)
    {
        Directory.CreateDirectory(folderPath);

        foreach (var child in node.Children)
        {
            var childPath = Path.Combine(folderPath, child.Name);
            CreateNode(childPath, child);
        }
    }

    private void CreateFileNode(string filePath, TemplateNode node)
    {
        var parentDirectoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(parentDirectoryPath))
        {
            throw new InvalidOperationException($"Cannot determine the parent directory for '{filePath}'.");
        }

        Directory.CreateDirectory(parentDirectoryPath);

        var overwritePolicy = NormalizeOverwritePolicy(node.OverwritePolicy);
        if (File.Exists(filePath))
        {
            switch (overwritePolicy)
            {
                case "skip":
                    return;

                case "error":
                    throw new IOException($"File already exists: {filePath}");

                case "overwrite":
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported overwrite policy '{node.OverwritePolicy}' for file '{filePath}'.");
            }
        }

        var content = node.ContentTemplate
            ?? throw new InvalidOperationException($"File node '{node.Name}' must define contentTemplate.");

        File.WriteAllText(filePath, content, ResolveEncoding(node.Encoding));
    }

    private static string NormalizeOverwritePolicy(string? overwritePolicy)
    {
        if (string.IsNullOrWhiteSpace(overwritePolicy))
        {
            return "error";
        }

        return overwritePolicy.Trim().ToLowerInvariant();
    }

    private static Encoding ResolveEncoding(string? encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName) ||
            string.Equals(encodingName, "utf8", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(encodingName, "utf-8", StringComparison.OrdinalIgnoreCase))
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Unsupported encoding '{encodingName}'.", ex);
        }
    }

    private static void ValidateNodeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Node name cannot be empty.");
        }

        if (name is "." or "..")
        {
            throw new InvalidOperationException($"Node name '{name}' is not allowed.");
        }

        if (!string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Node name '{name}' must not contain directory separator characters.");
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException(
                $"Node name '{name}' contains invalid file system characters.");
        }
    }
}
