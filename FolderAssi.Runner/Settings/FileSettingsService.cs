using System.Text.Json;

internal sealed class FileSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object _sync = new();
    private readonly string _defaultTemplatesPath;
    private AppSettings? _cached;

    public FileSettingsService(string settingsFilePath, string defaultTemplatesPath)
    {
        if (string.IsNullOrWhiteSpace(settingsFilePath))
        {
            throw new ArgumentException("settingsFilePath is required.", nameof(settingsFilePath));
        }

        if (string.IsNullOrWhiteSpace(defaultTemplatesPath))
        {
            throw new ArgumentException("defaultTemplatesPath is required.", nameof(defaultTemplatesPath));
        }

        SettingsFilePath = Path.GetFullPath(settingsFilePath);
        _defaultTemplatesPath = Path.GetFullPath(defaultTemplatesPath);
    }

    public string SettingsFilePath { get; }

    public AppSettings Get()
    {
        lock (_sync)
        {
            _cached ??= LoadOrCreate();
            return _cached;
        }
    }

    public AppSettingsView GetView()
    {
        var settings = Get();
        var effectiveApiKey = ResolveEffectiveApiKey(settings);
        return new AppSettingsView
        {
            Model = settings.Model,
            OpenAiEndpoint = settings.OpenAiEndpoint,
            TemplatesPath = settings.TemplatesPath,
            OutputPath = settings.OutputPath,
            ZipPath = settings.ZipPath,
            AiMode = ToAiModeString(settings.AiMode),
            HasApiKey = !string.IsNullOrWhiteSpace(effectiveApiKey),
            ApiKeyMasked = MaskApiKey(effectiveApiKey)
        };
    }

    public AppSettings Save(AppSettingsUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedAiMode = ParseAiMode(request.AiMode);
        if (!string.IsNullOrWhiteSpace(request.AiMode) && requestedAiMode is null)
        {
            throw new SettingsValidationException(
            [
                new SettingsValidationError
                {
                    Field = "aiMode",
                    Message = "aiMode must be either 'mock' or 'real'."
                }
            ]);
        }

        lock (_sync)
        {
            var current = _cached ??= LoadOrCreate();
            var merged = Merge(current, request, requestedAiMode);
            var normalized = Normalize(merged);

            var validationErrors = Validate(normalized);
            if (validationErrors.Count > 0)
            {
                throw new SettingsValidationException(validationErrors);
            }

            WriteToDisk(normalized);
            _cached = normalized;
            return normalized;
        }
    }

    private AppSettings LoadOrCreate()
    {
        if (!File.Exists(SettingsFilePath))
        {
            var defaultSettings = CreateDefaultSettings();
            WriteToDisk(defaultSettings);
            return defaultSettings;
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                ?? throw new InvalidOperationException("Settings file is empty.");

            var normalized = Normalize(loaded);
            var validationErrors = Validate(normalized);
            if (validationErrors.Count > 0)
            {
                throw new SettingsValidationException(validationErrors);
            }

            return normalized;
        }
        catch (SettingsValidationException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Settings JSON is invalid: {SettingsFilePath}", ex);
        }
    }

    private AppSettings CreateDefaultSettings()
    {
        var current = Environment.CurrentDirectory;

        return Normalize(new AppSettings
        {
            OpenAiApiKey = string.Empty,
            Model = "gpt-4o-mini",
            TemplatesPath = _defaultTemplatesPath,
            OutputPath = Path.Combine(current, "generated"),
            ZipPath = Path.Combine(current, "archives"),
            AiMode = AiMode.Mock
        });
    }

    private static AppSettings Merge(
        AppSettings current,
        AppSettingsUpdateRequest request,
        AiMode? requestedAiMode)
    {
        var openAiApiKey = current.OpenAiApiKey;
        if (request.ClearOpenAiApiKey)
        {
            openAiApiKey = string.Empty;
        }
        else if (!string.IsNullOrWhiteSpace(request.OpenAiApiKey))
        {
            openAiApiKey = request.OpenAiApiKey.Trim();
        }

        var aiMode = requestedAiMode ?? current.AiMode;

        return new AppSettings
        {
            OpenAiApiKey = openAiApiKey,
            Model = string.IsNullOrWhiteSpace(request.Model) ? current.Model : request.Model.Trim(),
            OpenAiEndpoint = string.IsNullOrWhiteSpace(request.OpenAiEndpoint) ? current.OpenAiEndpoint : request.OpenAiEndpoint.Trim(),
            TemplatesPath = string.IsNullOrWhiteSpace(request.TemplatesPath) ? current.TemplatesPath : request.TemplatesPath.Trim(),
            OutputPath = string.IsNullOrWhiteSpace(request.OutputPath) ? current.OutputPath : request.OutputPath.Trim(),
            ZipPath = string.IsNullOrWhiteSpace(request.ZipPath) ? current.ZipPath : request.ZipPath.Trim(),
            AiMode = aiMode
        };
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        return settings with
        {
            Model = settings.Model.Trim(),
            TemplatesPath = ToFullPath(settings.TemplatesPath),
            OutputPath = ToFullPath(settings.OutputPath),
            ZipPath = ToFullPath(settings.ZipPath),
            OpenAiApiKey = settings.OpenAiApiKey.Trim(),
            OpenAiEndpoint = string.IsNullOrWhiteSpace(settings.OpenAiEndpoint)
                ? "https://api.openai.com/v1/chat/completions"
                : settings.OpenAiEndpoint.Trim()
        };
    }

    private static string ToFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path.Trim());
    }

    private static List<SettingsValidationError> Validate(AppSettings settings)
    {
        var errors = new List<SettingsValidationError>();

        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            errors.Add(new SettingsValidationError
            {
                Field = "model",
                Message = "Model is required."
            });
        }

        if (string.IsNullOrWhiteSpace(settings.TemplatesPath))
        {
            errors.Add(new SettingsValidationError
            {
                Field = "templatesPath",
                Message = "Templates path is required."
            });
        }
        else if (!Directory.Exists(settings.TemplatesPath))
        {
            errors.Add(new SettingsValidationError
            {
                Field = "templatesPath",
                Message = $"Templates path was not found: {settings.TemplatesPath}"
            });
        }

        if (string.IsNullOrWhiteSpace(settings.OutputPath))
        {
            errors.Add(new SettingsValidationError
            {
                Field = "outputPath",
                Message = "Output path is required."
            });
        }

        if (string.IsNullOrWhiteSpace(settings.ZipPath))
        {
            errors.Add(new SettingsValidationError
            {
                Field = "zipPath",
                Message = "Zip path is required."
            });
        }

        if (string.IsNullOrWhiteSpace(settings.OpenAiEndpoint))
        {
            errors.Add(new SettingsValidationError
            {
                Field = "openAiEndpoint",
                Message = "Endpoint is required."
            });
        }
        else if (!Uri.TryCreate(settings.OpenAiEndpoint, UriKind.Absolute, out _))
        {
            errors.Add(new SettingsValidationError
            {
                Field = "openAiEndpoint",
                Message = "Endpoint must be a valid absolute URI."
            });
        }

        if (settings.AiMode == AiMode.Real)
        {
            var effectiveApiKey = ResolveEffectiveApiKey(settings);
            if (string.IsNullOrWhiteSpace(effectiveApiKey))
            {
                errors.Add(new SettingsValidationError
                {
                    Field = "openAiApiKey",
                    Message = "API Key is required when AI mode is real. (input field or OPENROUTER_API_KEY / OPENAI_API_KEY)"
                });
            }

            if (string.IsNullOrWhiteSpace(settings.Model))
            {
                errors.Add(new SettingsValidationError
                {
                    Field = "model",
                    Message = "Model is required when AI mode is real."
                });
            }

            if (settings.Model.Contains(":free", StringComparison.OrdinalIgnoreCase)
                && !settings.OpenAiEndpoint.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new SettingsValidationError
                {
                    Field = "openAiEndpoint",
                    Message = "':free' model variants require an OpenRouter endpoint (e.g. https://openrouter.ai/api/v1/chat/completions)."
                });
            }
        }

        return errors;
    }

    private static string ResolveEffectiveApiKey(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            return settings.OpenAiApiKey;
        }

        var openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (!string.IsNullOrWhiteSpace(openRouterApiKey))
        {
            return openRouterApiKey.Trim();
        }

        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(openAiApiKey))
        {
            return openAiApiKey.Trim();
        }

        return string.Empty;
    }

    private void WriteToDisk(AppSettings settings)
    {
        var directoryPath = Path.GetDirectoryName(SettingsFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException($"Invalid settings file path: {SettingsFilePath}");
        }

        Directory.CreateDirectory(directoryPath);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var tempFilePath = $"{SettingsFilePath}.tmp";
        File.WriteAllText(tempFilePath, json);

        if (File.Exists(SettingsFilePath))
        {
            File.Delete(SettingsFilePath);
        }

        File.Move(tempFilePath, SettingsFilePath);
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return string.Empty;
        }

        if (apiKey.Length <= 4)
        {
            return "****";
        }

        return $"****{apiKey[^4..]}";
    }

    private static AiMode? ParseAiMode(string? aiModeText)
    {
        if (string.IsNullOrWhiteSpace(aiModeText))
        {
            return null;
        }

        return aiModeText.Trim().ToLowerInvariant() switch
        {
            "mock" => AiMode.Mock,
            "real" => AiMode.Real,
            _ => null
        };
    }

    private static string ToAiModeString(AiMode aiMode)
    {
        return aiMode == AiMode.Real ? "real" : "mock";
    }
}
