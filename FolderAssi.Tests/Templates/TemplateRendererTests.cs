using FolderAssi.Domain.Templates;
using FolderAssi.Infrastructure.Templates;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Templates;

public sealed class TemplateRendererTests
{
    private readonly TemplateRenderer _renderer = new(new VariableResolver());

    [Fact]
    public void Render_ReplacesVariablesInNameAndContent()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi"
        };
        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = false
        };

        var rendered = _renderer.Render(template, variables, options);

        Assert.Equal("MyAwesomeApi", rendered.Name);
        var programFile = rendered.Children.Single(node => node.Name == "Program.cs");
        Assert.Equal(TemplateNodeType.File, programFile.Type);
        Assert.Contains("namespace MyAwesomeApi;", programFile.ContentTemplate);
    }

    [Fact]
    public void Render_WhenOptionalConditionTrue_IncludesOptionalNode()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi"
        };
        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true
        };

        var rendered = _renderer.Render(template, variables, options);

        Assert.Contains(rendered.Children, child => child.Name == "Auth");
    }

    [Fact]
    public void Render_WhenOptionalConditionFalse_ExcludesOptionalNode()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi"
        };
        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = false
        };

        var rendered = _renderer.Render(template, variables, options);

        Assert.DoesNotContain(rendered.Children, child => child.Name == "Auth");
    }

    [Fact]
    public void Render_WhenOptionalNodeMissingConditionKey_ThrowsInvalidOperationException()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        template.Root.Children.Add(new TemplateNode
        {
            Name = "BadOptional",
            Type = TemplateNodeType.Folder,
            Optional = true,
            ConditionKey = string.Empty
        });

        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi"
        };
        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true
        };

        var ex = Assert.Throws<InvalidOperationException>(() => _renderer.Render(template, variables, options));
        Assert.Contains("must define conditionKey", ex.Message);
    }

    [Fact]
    public void Render_DoesNotMutateOriginalTemplate()
    {
        var template = TestTemplateFactory.CreateAspNetTemplate();
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi"
        };
        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true
        };

        _ = _renderer.Render(template, variables, options);

        Assert.Equal("{{projectName}}", template.Root.Name);
        var originalProgram = template.Root.Children.Single(node => node.Name == "Program.cs");
        Assert.Contains("{{namespace}}", originalProgram.ContentTemplate);
    }
}
