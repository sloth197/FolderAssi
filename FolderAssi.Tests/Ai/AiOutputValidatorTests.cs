using FolderAssi.Domain.Ai;
using FolderAssi.Infrastructure.Ai;
using FolderAssi.Tests.TestHelpers;

namespace FolderAssi.Tests.Ai;

public sealed class AiOutputValidatorTests
{
    private readonly AiOutputValidator _validator = new();

    [Fact]
    public void Validate_WhenTemplateIdUnknown_ReturnsTemplateIdUnknownError()
    {
        var result = new TemplateRecommendationResult
        {
            TemplateId = "unknown-template",
            Confidence = 0.8d
        };

        var validation = _validator.Validate(result, TestTemplateFactory.CreateCoreTemplateSet());

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Code == "TEMPLATE_ID_UNKNOWN");
    }

    [Fact]
    public void Validate_WhenRequiredVariableMissing_ReturnsVariableRequiredMissingError()
    {
        var templateWithoutNamespaceDefault = TestTemplateFactory.CreateAspNetTemplate() with
        {
            DefaultVariables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyApi"
            }
        };

        var result = new TemplateRecommendationResult
        {
            TemplateId = "aspnetcore-webapi-starter",
            Variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyApi"
            },
            Options = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["includeAuth"] = true
            },
            Confidence = 0.9d
        };

        var validation = _validator.Validate(result, [templateWithoutNamespaceDefault]);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Code == "VARIABLE_REQUIRED_MISSING");
    }

    [Fact]
    public void Validate_WhenOptionKeyNotDefined_ReturnsOptionKeyInvalidError()
    {
        var result = new TemplateRecommendationResult
        {
            TemplateId = "aspnetcore-webapi-starter",
            Variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyApi",
                ["namespace"] = "MyApi"
            },
            Options = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["includeSwagger"] = true
            },
            Confidence = 0.9d
        };

        var validation = _validator.Validate(result, TestTemplateFactory.CreateCoreTemplateSet());

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Code == "OPTION_KEY_INVALID");
    }

    [Fact]
    public void Validate_WhenConfidenceOutOfRange_ReturnsConfidenceOutOfRangeError()
    {
        var result = new TemplateRecommendationResult
        {
            TemplateId = "aspnetcore-webapi-starter",
            Variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyApi",
                ["namespace"] = "MyApi"
            },
            Options = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["includeAuth"] = true
            },
            Confidence = 1.5d
        };

        var validation = _validator.Validate(result, TestTemplateFactory.CreateCoreTemplateSet());

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Code == "CONFIDENCE_OUT_OF_RANGE");
    }

    [Fact]
    public void Validate_WithValidRecommendation_ReturnsSuccess()
    {
        var result = new TemplateRecommendationResult
        {
            TemplateId = "aspnetcore-webapi-starter",
            Variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyApi",
                ["namespace"] = "MyApi"
            },
            Options = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["includeAuth"] = true
            },
            Confidence = 0.9d
        };

        var validation = _validator.Validate(result, TestTemplateFactory.CreateCoreTemplateSet());

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }
}
