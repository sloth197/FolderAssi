using FolderAssi.Application.Templates;
using FolderAssi.Domain.Templates;

namespace FolderAssi.Infrastructure.Templates;

public sealed class TemplateRenderer : ITemplateRenderer
{
    private readonly IVariableResolver _variableResolver;

    public TemplateRenderer(IVariableResolver variableResolver)
    {
        _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
    }

    public TemplateNode Render(
        ProjectTemplate template,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, object?> options)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(options);

        if (template.Root == null)
        {
            throw new InvalidOperationException("Template root is required for rendering.");
        }

        var renderedRoot = RenderNode(template.Root, variables, options);
        return renderedRoot ?? throw new InvalidOperationException("Rendered root cannot be null.");
    }

    private TemplateNode? RenderNode(
        TemplateNode node,
        IReadOnlyDictionary<string, string> variables,
        IReadOnlyDictionary<string, object?> options)
    {
        if (node.Optional)
        {
            if (string.IsNullOrWhiteSpace(node.ConditionKey))
            {
                throw new InvalidOperationException(
                    $"Optional node '{node.Name}' must define conditionKey.");
            }

            if (!options.TryGetValue(node.ConditionKey, out var optionValue))
            {
                return null;
            }

            if (optionValue is not bool enabled || !enabled)
            {
                return null;
            }
        }

        var renderedName = _variableResolver.Resolve(node.Name, variables);

        var renderedContent = node.Type == TemplateNodeType.File
            ? ResolveFileContent(node, variables)
            : null;

        var renderedChildren = new List<TemplateNode>();
        foreach (var child in node.Children)
        {
            var renderedChild = RenderNode(child, variables, options);
            if (renderedChild is not null)
            {
                renderedChildren.Add(renderedChild);
            }
        }

        return new TemplateNode
        {
            Name = renderedName,
            Type = node.Type,
            Description = node.Description,
            Optional = node.Optional,
            ConditionKey = node.ConditionKey,
            Children = renderedChildren,
            ContentTemplate = renderedContent,
            Encoding = node.Encoding,
            OverwritePolicy = node.OverwritePolicy,
            IsBinary = node.IsBinary,
        };
    }

    private string ResolveFileContent(
        TemplateNode node,
        IReadOnlyDictionary<string, string> variables)
    {
        if (node.ContentTemplate == null)
        {
            throw new InvalidOperationException(
                $"File node '{node.Name}' must define contentTemplate.");
        }

        return _variableResolver.Resolve(node.ContentTemplate, variables);
    }
}
