using FolderAssi.Application.Templates;
using FolderAssi.Domain.Templates;
using FolderAssi.Infrastructure.Templates;

internal static class TemplateRenderingVerification
{
    public static void Run(string templatesPath)
    {
        Console.WriteLine("== VariableResolver + TemplateRenderer Verification ==");

        ITemplateLoader templateLoader = new JsonTemplateLoader(templatesPath);
        ITemplateValidator templateValidator = new TemplateValidator();
        IVariableResolver variableResolver = new VariableResolver();
        ITemplateRenderer templateRenderer = new TemplateRenderer(variableResolver);

        var template = templateLoader.GetById("aspnetcore-webapi-starter");
        var templateValidation = templateValidator.Validate(template);
        if (!templateValidation.IsValid)
        {
            Console.WriteLine("Template validation failed before rendering tests.");
            foreach (var error in templateValidation.Errors)
            {
                Console.WriteLine($"- [{error.Code}] {error.Path}: {error.Message}");
            }

            return;
        }

        var passed = 0;
        var failed = 0;

        if (VerifyVariableSubstitution(templateRenderer, template))
        {
            passed++;
        }
        else
        {
            failed++;
        }

        if (VerifyOptionalNode(templateRenderer, template))
        {
            passed++;
        }
        else
        {
            failed++;
        }

        if (VerifyMissingVariableException(templateRenderer, template))
        {
            passed++;
        }
        else
        {
            failed++;
        }

        if (VerifyMissingConditionKeyException(templateRenderer))
        {
            passed++;
        }
        else
        {
            failed++;
        }

        Console.WriteLine($"Summary: PASS={passed}, FAIL={failed}");
    }

    private static bool VerifyVariableSubstitution(ITemplateRenderer renderer, ProjectTemplate template)
    {
        Console.WriteLine("[1] Variable substitution");

        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi",
        };

        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true,
        };

        var renderedRoot = renderer.Render(template, variables, options);
        var controllerFile = FindNode(renderedRoot, "WeatherForecastController.cs");

        var rootNameOk = string.Equals(renderedRoot.Name, "MyAwesomeApi", StringComparison.Ordinal);
        var namespaceOk = controllerFile?.ContentTemplate?.Contains(
            "namespace MyAwesomeApi.Controllers;",
            StringComparison.Ordinal) == true;

        PrintCheck("{{projectName}} -> MyAwesomeApi", rootNameOk);
        PrintCheck("{{namespace}} -> MyAwesomeApi", namespaceOk);

        return rootNameOk && namespaceOk;
    }

    private static bool VerifyOptionalNode(ITemplateRenderer renderer, ProjectTemplate template)
    {
        Console.WriteLine("[2] Optional node includeAuth condition");

        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi",
        };

        var includeAuthTrue = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true,
        };

        var includeAuthFalse = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = false,
        };

        var renderedWhenTrue = renderer.Render(template, variables, includeAuthTrue);
        var renderedWhenFalse = renderer.Render(template, variables, includeAuthFalse);

        var authIncluded = FindNode(renderedWhenTrue, "Auth") is not null;
        var authExcluded = FindNode(renderedWhenFalse, "Auth") is null;

        PrintCheck("includeAuth=true -> Auth included", authIncluded);
        PrintCheck("includeAuth=false -> Auth excluded", authExcluded);

        return authIncluded && authExcluded;
    }

    private static bool VerifyMissingVariableException(ITemplateRenderer renderer, ProjectTemplate template)
    {
        Console.WriteLine("[3] Missing variable exception");

        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            // namespace intentionally omitted
        };

        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true,
        };

        try
        {
            _ = renderer.Render(template, variables, options);
            PrintCheck("Missing variable throws InvalidOperationException", false);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  Message: {ex.Message}");
            PrintCheck("Missing variable throws InvalidOperationException", true);
            return true;
        }
    }

    private static bool VerifyMissingConditionKeyException(ITemplateRenderer renderer)
    {
        Console.WriteLine("[4] optional=true without conditionKey exception");

        var template = BuildTemplateWithInvalidOptionalNode();
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectName"] = "MyAwesomeApi",
            ["namespace"] = "MyAwesomeApi",
        };

        var options = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeAuth"] = true,
        };

        try
        {
            _ = renderer.Render(template, variables, options);
            PrintCheck("Missing conditionKey throws InvalidOperationException", false);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  Message: {ex.Message}");
            PrintCheck("Missing conditionKey throws InvalidOperationException", true);
            return true;
        }
    }

    private static ProjectTemplate BuildTemplateWithInvalidOptionalNode()
    {
        return new ProjectTemplate
        {
            Id = "invalid-optional-template",
            Name = "Invalid Optional Template",
            Description = "Template for renderer exception verification.",
            Language = "C#",
            Framework = "ASP.NET Core",
            TemplateVersion = "1.0.0",
            RequiredVariables = ["projectName", "namespace"],
            Root = new TemplateNode
            {
                Name = "{{projectName}}",
                Type = TemplateNodeType.Folder,
                Children =
                [
                    new TemplateNode
                    {
                        Name = "Auth",
                        Type = TemplateNodeType.Folder,
                        Optional = true,
                        ConditionKey = null,
                        Children = []
                    }
                ]
            }
        };
    }

    private static TemplateNode? FindNode(TemplateNode node, string name)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindNode(child, name);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static void PrintCheck(string label, bool passed)
    {
        Console.WriteLine($"- {label}: {(passed ? "PASS" : "FAIL")}");
    }
}
