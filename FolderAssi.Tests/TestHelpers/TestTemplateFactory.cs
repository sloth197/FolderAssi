using FolderAssi.Domain.Ai;
using FolderAssi.Domain.Templates;

namespace FolderAssi.Tests.TestHelpers;

internal static class TestTemplateFactory
{
    public static ProjectTemplate CreateAspNetTemplate()
    {
        return new ProjectTemplate
        {
            Id = "aspnetcore-webapi-starter",
            Name = "ASP.NET Core Web API Starter",
            Description = "ASP.NET Core API template",
            Language = "C#",
            Framework = "ASP.NET Core",
            TemplateVersion = "1.0.0",
            Tags = ["backend", "api", "aspnet", "jwt"],
            DefaultVariables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "MyAwesomeApi",
                ["namespace"] = "MyAwesomeApi"
            },
            RequiredVariables = ["projectName", "namespace"],
            Options =
            [
                new TemplateOption
                {
                    Key = "includeAuth",
                    Label = "Include Auth",
                    Type = "boolean",
                    Default = false
                }
            ],
            Root = new TemplateNode
            {
                Name = "{{projectName}}",
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
                        ContentTemplate = "namespace {{namespace}};\\npublic class Program { }",
                        OverwritePolicy = "overwrite"
                    },
                    new TemplateNode
                    {
                        Name = "Auth",
                        Type = TemplateNodeType.Folder,
                        Optional = true,
                        ConditionKey = "includeAuth",
                        Children =
                        [
                            new TemplateNode
                            {
                                Name = "JwtTokenService.cs",
                                Type = TemplateNodeType.File,
                                ContentTemplate = "namespace {{namespace}}.Auth;\\npublic sealed class JwtTokenService { }",
                                OverwritePolicy = "overwrite"
                            }
                        ]
                    }
                ]
            }
        };
    }

    public static ProjectTemplate CreateSpringTemplate()
    {
        return new ProjectTemplate
        {
            Id = "spring-boot-layered-api-starter",
            Name = "Spring Boot Layered API Starter",
            Description = "Spring API template",
            Language = "Java",
            Framework = "Spring Boot",
            TemplateVersion = "1.0.0",
            Tags = ["backend", "api", "spring", "java", "swagger"],
            DefaultVariables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "my-spring-api",
                ["packageName"] = "com.mycompany.demo",
                ["mainClassName"] = "Application"
            },
            RequiredVariables = ["projectName", "packageName", "mainClassName"],
            Options =
            [
                new TemplateOption
                {
                    Key = "includeAuth",
                    Label = "Include Auth",
                    Type = "boolean",
                    Default = false
                },
                new TemplateOption
                {
                    Key = "includeSwagger",
                    Label = "Include Swagger",
                    Type = "boolean",
                    Default = false
                }
            ],
            Root = new TemplateNode
            {
                Name = "{{projectName}}",
                Type = TemplateNodeType.Folder,
                Children =
                [
                    new TemplateNode
                    {
                        Name = "README.md",
                        Type = TemplateNodeType.File,
                        ContentTemplate = "# {{projectName}}"
                    }
                ]
            }
        };
    }

    public static ProjectTemplate CreateReactTemplate()
    {
        return new ProjectTemplate
        {
            Id = "react-feature-based-starter",
            Name = "React Feature Based Starter",
            Description = "React frontend template",
            Language = "TypeScript",
            Framework = "React",
            TemplateVersion = "1.0.0",
            Tags = ["frontend", "react", "spa"],
            DefaultVariables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["projectName"] = "my-react-app"
            },
            RequiredVariables = ["projectName"],
            Options =
            [
                new TemplateOption
                {
                    Key = "includeApi",
                    Label = "Include API Client",
                    Type = "boolean",
                    Default = false
                }
            ],
            Root = new TemplateNode
            {
                Name = "{{projectName}}",
                Type = TemplateNodeType.Folder,
                Children =
                [
                    new TemplateNode
                    {
                        Name = "src",
                        Type = TemplateNodeType.Folder,
                        Children =
                        [
                            new TemplateNode
                            {
                                Name = "main.tsx",
                                Type = TemplateNodeType.File,
                                ContentTemplate = "console.log('{{projectName}}');"
                            }
                        ]
                    }
                ]
            }
        };
    }

    public static IReadOnlyList<ProjectTemplate> CreateCoreTemplateSet()
    {
        return
        [
            CreateAspNetTemplate(),
            CreateSpringTemplate(),
            CreateReactTemplate()
        ];
    }

    public static TemplateCandidate ToCandidate(ProjectTemplate template)
    {
        return new TemplateCandidate
        {
            Id = template.Id,
            Name = template.Name,
            Language = template.Language,
            Framework = template.Framework,
            Tags = template.Tags.ToList(),
            RequiredVariables = template.RequiredVariables.ToList(),
            SupportedOptionKeys = template.Options.Select(static option => option.Key).ToList()
        };
    }
}
