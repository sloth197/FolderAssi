namespace FolderAssi.Tests.TestHelpers;

internal sealed class TemporaryDirectory : IDisposable
{
    private bool _disposed;

    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "FolderAssi.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CreateSubdirectory(string name)
    {
        var directoryPath = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    public string GetPath(params string[] segments)
    {
        return segments.Aggregate(Path, System.IO.Path.Combine);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in test teardown.
        }
    }
}
