internal enum AiMode
{
    Mock = 1,
    Real = 2
}

internal sealed record class AppSettings
{
    public string OpenAiApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o-mini";
    public string TemplatesPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string ZipPath { get; init; } = string.Empty;
    public AiMode AiMode { get; init; } = AiMode.Mock;
    public string OpenAiEndpoint { get; init; } = "https://api.openai.com/v1/chat/completions";
}

internal sealed record class AppSettingsView
{
    public string Model { get; init; } = string.Empty;
    public string OpenAiEndpoint { get; init; } = string.Empty;
    public string TemplatesPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string ZipPath { get; init; } = string.Empty;
    public string AiMode { get; init; } = "mock";
    public bool HasApiKey { get; init; }
    public string ApiKeyMasked { get; init; } = string.Empty;
}

internal sealed record class AppSettingsUpdateRequest
{
    public string? OpenAiApiKey { get; init; }
    public bool ClearOpenAiApiKey { get; init; }
    public string? Model { get; init; }
    public string? OpenAiEndpoint { get; init; }
    public string? TemplatesPath { get; init; }
    public string? OutputPath { get; init; }
    public string? ZipPath { get; init; }
    public string? AiMode { get; init; }
}

internal sealed record class SettingsValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

internal sealed class SettingsValidationException : Exception
{
    public SettingsValidationException(IReadOnlyList<SettingsValidationError> errors)
        : base("Settings validation failed.")
    {
        Errors = errors;
    }

    public IReadOnlyList<SettingsValidationError> Errors { get; }
}
