using FolderAssi.Infrastructure.Templates;

namespace FolderAssi.Tests.Templates;

public sealed class VariableResolverTests
{
    private readonly VariableResolver _resolver = new();

    [Fact]
    public void Resolve_ReplacesAllPlaceholders()
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi"
        };

        var result = _resolver.Resolve(
            "{{projectName}}/Program.cs => namespace {{namespace}};",
            variables);

        Assert.Equal("MyAwesomeApi/Program.cs => namespace MyAwesomeApi;", result);
    }

    [Fact]
    public void Resolve_WhenRequiredVariableMissing_ThrowsInvalidOperationException()
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _resolver.Resolve("namespace {{namespace}};", variables));

        Assert.Contains("Variable 'namespace' was not provided or is empty", ex.Message);
    }

    [Fact]
    public void Resolve_WhenVariableNameInvalid_ThrowsInvalidOperationException()
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _resolver.Resolve("{{project-name}}", variables));

        Assert.Contains("is invalid", ex.Message);
    }
}
