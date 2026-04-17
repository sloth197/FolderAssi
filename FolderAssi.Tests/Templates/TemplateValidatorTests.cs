using FolderAssi.Domain.Templates;
using FolderAssi.Infrastructure.Templates;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Templates;

public sealed class TemplateValidatorTests
{
    private readonly TemplateValidator _validator = new();

    [Fact]
    public void Validate_WithValidTemplate_ReturnsSuccess()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();

        var result = _validator.Validate(template);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenRootIsFile_ReturnsRootNotFolderError()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate() with
        {
            Root = new TemplateNode
            {
                Name = "Program.cs",
                Type = TemplateNodeType.File,
                ContentTemplate = "Console.WriteLine(\"Hello\");"
            }
        };

        var result = _validator.Validate(template);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "ROOT_NOT_FOLDER");
    }

    [Fact]
    public void Validate_WhenFileHasChildren_ReturnsFileHasChildrenError()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        template.Root.Children.Add(new TemplateNode
        {
            Name = "BadFile.cs",
            Type = TemplateNodeType.File,
            ContentTemplate = "class BadFile {}",
            Children =
            [
                new TemplateNode
                {
                    Name = "Nested",
                    Type = TemplateNodeType.Folder
                }
            ]
        });

        var result = _validator.Validate(template);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "FILE_HAS_CHILDREN");
    }

    [Fact]
    public void Validate_WhenOptionalNodeHasNoConditionKey_ReturnsError()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        template.Root.Children.Add(new TemplateNode
        {
            Name = "OptionalFolder",
            Type = TemplateNodeType.Folder,
            Optional = true,
            ConditionKey = string.Empty
        });

        var result = _validator.Validate(template);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "OPTIONAL_NODE_MISSING_CONDITION");
    }

    [Fact]
    public void Validate_WhenSiblingNamesDuplicate_ReturnsDuplicateChildNameError()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        template.Root.Children.Add(new TemplateNode
        {
            Name = "Controllers",
            Type = TemplateNodeType.Folder
        });

        var result = _validator.Validate(template);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "DUPLICATE_CHILD_NAME");
    }
}
