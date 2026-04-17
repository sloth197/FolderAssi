using FolderAssi.Domain.Templates;
using FolderAssi.Infrastructure.Scaffolding;

namespace FolderAssi.Tests.Scaffolding;

public sealed class FileSystemScaffoldBuilderTests
{
    private readonly FileSystemScaffoldBuilder _builder = new();

    [Fact]
    public void BuildProject_CreatesFoldersAndFilesRecursively()
    {
        using var temp = new TestHelpers.TemporaryDirectory();
        var root = CreateRenderedTree("MyApp", overwritePolicy: "overwrite", content: "first");

        var projectPath = _builder.BuildProject(temp.Path, root);

        Assert.True(Directory.Exists(projectPath));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "Controllers")));
        var programPath = Path.Combine(projectPath, "Program.cs");
        Assert.True(File.Exists(programPath));
        Assert.Equal("first", File.ReadAllText(programPath));
    }

    [Fact]
    public void BuildProject_WithSkipOverwritePolicy_DoesNotOverwriteExistingFile()
    {
        using var temp = new TestHelpers.TemporaryDirectory();
        var first = CreateRenderedTree("MyApp", overwritePolicy: "overwrite", content: "v1");
        var second = CreateRenderedTree("MyApp", overwritePolicy: "skip", content: "v2");

        var projectPath = _builder.BuildProject(temp.Path, first);
        _ = _builder.BuildProject(temp.Path, second);

        var programPath = Path.Combine(projectPath, "Program.cs");
        Assert.Equal("v1", File.ReadAllText(programPath));
    }

    [Fact]
    public void BuildProject_WithErrorOverwritePolicy_ThrowsIOException()
    {
        using var temp = new TestHelpers.TemporaryDirectory();
        var first = CreateRenderedTree("MyApp", overwritePolicy: "overwrite", content: "v1");
        var second = CreateRenderedTree("MyApp", overwritePolicy: "error", content: "v2");

        _ = _builder.BuildProject(temp.Path, first);

        var ex = Assert.Throws<IOException>(() => _builder.BuildProject(temp.Path, second));
        Assert.Contains("File already exists", ex.Message);
    }

    [Fact]
    public void BuildProject_WhenRootIsNotFolder_ThrowsInvalidOperationException()
    {
        using var temp = new TestHelpers.TemporaryDirectory();
        var invalidRoot = new TemplateNode
        {
            Name = "Program.cs",
            Type = TemplateNodeType.File,
            ContentTemplate = "content"
        };

        var ex = Assert.Throws<InvalidOperationException>(() => _builder.BuildProject(temp.Path, invalidRoot));
        Assert.Contains("must be a folder", ex.Message);
    }

    private static TemplateNode CreateRenderedTree(string rootName, string overwritePolicy, string content)
    {
        return new TemplateNode
        {
            Name = rootName,
            Type = TemplateNodeType.Folder,
            Children =
            [
                new TemplateNode
                {
                    Name = "Controllers",
                    Type = TemplateNodeType.Folder
                },
                new TemplateNode
                {
                    Name = "Program.cs",
                    Type = TemplateNodeType.File,
                    ContentTemplate = content,
                    OverwritePolicy = overwritePolicy,
                    Encoding = "utf-8"
                }
            ]
        };
    }
}
