using System.IO.Compression;
using FolderAssi.Application.Scaffolding;

namespace FolderAssi.Infrastructure.Scaffolding;

public sealed class ZipArchiveService : IArchiveService
{
    public string CreateZip(string sourceDirectoryPath, string destinationZipPath)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectoryPath))
        {
            throw new ArgumentException("Source directory path is required.", nameof(sourceDirectoryPath));
        }

        if (string.IsNullOrWhiteSpace(destinationZipPath))
        {
            throw new ArgumentException("Destination zip path is required.", nameof(destinationZipPath));
        }

        var normalizedSourceDirectoryPath = Path.GetFullPath(sourceDirectoryPath);
        var normalizedDestinationZipPath = Path.GetFullPath(destinationZipPath);

        if (!Directory.Exists(normalizedSourceDirectoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Source directory was not found: {normalizedSourceDirectoryPath}");
        }

        var destinationDirectoryPath = Path.GetDirectoryName(normalizedDestinationZipPath);
        if (string.IsNullOrWhiteSpace(destinationDirectoryPath))
        {
            throw new InvalidOperationException(
                $"Cannot determine the destination directory for '{normalizedDestinationZipPath}'.");
        }

        Directory.CreateDirectory(destinationDirectoryPath);

        if (File.Exists(normalizedDestinationZipPath))
        {
            File.Delete(normalizedDestinationZipPath);
        }

        ZipFile.CreateFromDirectory(
            normalizedSourceDirectoryPath,
            normalizedDestinationZipPath,
            CompressionLevel.Optimal,
            includeBaseDirectory: true);

        return normalizedDestinationZipPath;
    }
}
