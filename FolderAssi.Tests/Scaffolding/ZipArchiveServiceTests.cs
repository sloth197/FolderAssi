using System.IO.Compression;
using FolderAssi.Infrastructure.Scaffolding;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Scaffolding;

public sealed class ZipArchiveServiceTests
{
    private readonly ZipArchiveService _service = new();

    [Fact]
    public void CreateZip_CreatesZipWithBaseDirectory()
    {
        using var temp = new TemporaryDirectory();
        var sourceRoot = temp.CreateSubdirectory("MyApp");
        var sourceFilePath = Path.Combine(sourceRoot, "Program.cs");
        File.WriteAllText(sourceFilePath, "Console.WriteLine(\"Hello\");");

        var zipPath = temp.GetPath("archives", "MyApp.zip");
        var resultPath = _service.CreateZip(sourceRoot, zipPath);

        Assert.True(File.Exists(resultPath));

        using var archive = ZipFile.OpenRead(resultPath);
        Assert.Contains(archive.Entries, entry => entry.FullName.EndsWith("MyApp/Program.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateZip_WhenSourceDirectoryMissing_ThrowsDirectoryNotFoundException()
    {
        using var temp = new TemporaryDirectory();
        var missingSource = temp.GetPath("missing");
        var zipPath = temp.GetPath("archives", "output.zip");

        var ex = Assert.Throws<DirectoryNotFoundException>(() => _service.CreateZip(missingSource, zipPath));
        Assert.Contains("Source directory was not found", ex.Message);
    }
}
