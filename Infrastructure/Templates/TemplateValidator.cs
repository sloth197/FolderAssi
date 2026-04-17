using FolderAssi.Application.Templates;
using FolderAssi.Domain.Templates;
using FolderAssi.Domain.Validation;

namespace FolderAssi.Infrastructure.Templates;

public sealed class TemplateValidator : ITemplateValidator
{
    public ValidationResult Validate(ProjectTemplate template)
    {
        var result = new ValidationResult();

        if (template is null)
        {
            result.AddError("TEMPLATE_NULL", "Template cannot be null.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(template.Id))
        {
            result.AddError("TEMPLATE_ID_EMPTY", "ProjectTemplate.Id is required.");
        }

        if (string.IsNullOrWhiteSpace(template.Name))
        {
            result.AddError("TEMPLATE_NAME_EMPTY", "ProjectTemplate.Name is required.");
        }

        if (string.IsNullOrWhiteSpace(template.Language))
        {
            result.AddError("TEMPLATE_LANGUAGE_EMPTY", "ProjectTemplate.Language is required.");
        }

        if (string.IsNullOrWhiteSpace(template.Framework))
        {
            result.AddError("TEMPLATE_FRAMEWORK_EMPTY", "ProjectTemplate.Framework is required.");
        }

        if (template.Root is null)
        {
            result.AddError("TEMPLATE_ROOT_NULL", "ProjectTemplate.Root is required.");
            return result;
        }

        if (template.Root.Type != TemplateNodeType.Folder)
        {
            result.AddError("ROOT_NOT_FOLDER", "ProjectTemplate.Root must be folder type.", template.Root.Name);
        }

        ValidateNode(template.Root, result, template.Root.Name);
        return result;
    }

    private static void ValidateNode(
        TemplateNode node,
        ValidationResult result,
        string currentPath)
    {
        if (string.IsNullOrWhiteSpace(node.Name))
        {
            result.AddError("NODE_NAME_EMPTY", "TemplateNode.Name is required.", currentPath);
        }

        if (node.Type == TemplateNodeType.File)
        {
            if (node.Children.Count > 0)
            {
                result.AddError("FILE_HAS_CHILDREN", "File node cannot have child nodes.", currentPath);
            }

            if (string.IsNullOrWhiteSpace(node.ContentTemplate))
            {
                result.AddError("FILE_CONTENT_EMPTY", "File node must define contentTemplate.", currentPath);
            }
        }

        if (node.Optional && string.IsNullOrWhiteSpace(node.ConditionKey))
        {
            result.AddError(
                "OPTIONAL_NODE_MISSING_CONDITION",
                "Optional node must define conditionKey.",
                currentPath);
        }

        ValidateDuplicateNames(node, result, currentPath);

        foreach (var child in node.Children)
        {
            var childPath = CombinePath(currentPath, child.Name);
            ValidateNode(child, result, childPath);
        }
    }

    private static void ValidateDuplicateNames(
        TemplateNode node,
        ValidationResult result,
        string currentPath)
    {
        if (node.Children.Count == 0)
        {
            return;
        }

        var duplicateGroups = node.Children
            .GroupBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            result.AddError(
                "DUPLICATE_CHILD_NAME",
                $"Duplicate child node name '{group.Key}' found under the same folder.",
                currentPath);
        }
    }

    private static string CombinePath(string parent, string child)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return child;
        }

        if (string.IsNullOrWhiteSpace(child))
        {
            return parent;
        }

        return $"{parent}/{child}";
    }
}
