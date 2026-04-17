using System.Text.Json;

internal sealed class FileHistoryService : IHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object _sync = new();
    private readonly int _maxEntries;
    private List<GenerationHistoryEntry>? _cached;

    public FileHistoryService(string historyFilePath, int maxEntries = 200)
    {
        if (string.IsNullOrWhiteSpace(historyFilePath))
        {
            throw new ArgumentException("historyFilePath is required.", nameof(historyFilePath));
        }

        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "maxEntries must be greater than zero.");
        }

        HistoryFilePath = Path.GetFullPath(historyFilePath);
        _maxEntries = maxEntries;
    }

    public string HistoryFilePath { get; }

    public IReadOnlyList<GenerationHistoryEntry> GetRecent(int limit = 20)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "limit must be greater than zero.");
        }

        lock (_sync)
        {
            _cached ??= LoadOrCreate();
            return _cached
                .OrderByDescending(static item => item.CreatedAtUtc)
                .Take(limit)
                .Select(CloneEntry)
                .ToList();
        }
    }

    public void Add(GenerationHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_sync)
        {
            _cached ??= LoadOrCreate();

            var normalized = Normalize(entry);
            _cached.Insert(0, normalized);

            if (_cached.Count > _maxEntries)
            {
                _cached = _cached
                    .Take(_maxEntries)
                    .ToList();
            }

            WriteToDisk(_cached);
        }
    }

    private List<GenerationHistoryEntry> LoadOrCreate()
    {
        var directoryPath = Path.GetDirectoryName(HistoryFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException($"Invalid history file path: {HistoryFilePath}");
        }

        Directory.CreateDirectory(directoryPath);

        if (!File.Exists(HistoryFilePath))
        {
            var empty = new List<GenerationHistoryEntry>();
            WriteToDisk(empty);
            return empty;
        }

        try
        {
            var json = File.ReadAllText(HistoryFilePath);
            var loaded = JsonSerializer.Deserialize<List<GenerationHistoryEntry>>(json, JsonOptions)
                ?? [];

            return loaded
                .Select(CloneEntry)
                .OrderByDescending(static item => item.CreatedAtUtc)
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"History JSON is invalid: {HistoryFilePath}", ex);
        }
    }

    private void WriteToDisk(IReadOnlyList<GenerationHistoryEntry> entries)
    {
        var directoryPath = Path.GetDirectoryName(HistoryFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException($"Invalid history file path: {HistoryFilePath}");
        }

        Directory.CreateDirectory(directoryPath);

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        var tempFilePath = $"{HistoryFilePath}.tmp";
        File.WriteAllText(tempFilePath, json);

        if (File.Exists(HistoryFilePath))
        {
            File.Delete(HistoryFilePath);
        }

        File.Move(tempFilePath, HistoryFilePath);
    }

    private static GenerationHistoryEntry Normalize(GenerationHistoryEntry entry)
    {
        var id = string.IsNullOrWhiteSpace(entry.Id)
            ? Guid.NewGuid().ToString("N")
            : entry.Id.Trim();

        return new GenerationHistoryEntry
        {
            Id = id,
            CreatedAtUtc = entry.CreatedAtUtc == default ? DateTimeOffset.UtcNow : entry.CreatedAtUtc.ToUniversalTime(),
            UserInput = entry.UserInput?.Trim() ?? string.Empty,
            SelectedTemplateId = entry.SelectedTemplateId?.Trim() ?? string.Empty,
            Variables = new Dictionary<string, string>(
                entry.Variables ?? new Dictionary<string, string>(StringComparer.Ordinal),
                StringComparer.Ordinal),
            Options = new Dictionary<string, object?>(
                entry.Options ?? new Dictionary<string, object?>(StringComparer.Ordinal),
                StringComparer.Ordinal),
            GeneratedProjectPath = NormalizePath(entry.GeneratedProjectPath),
            GeneratedZipPath = NormalizePath(entry.GeneratedZipPath),
            Success = entry.Success,
            FailureReason = entry.FailureReason?.Trim() ?? string.Empty
        };
    }

    private static GenerationHistoryEntry CloneEntry(GenerationHistoryEntry entry)
    {
        return new GenerationHistoryEntry
        {
            Id = entry.Id,
            CreatedAtUtc = entry.CreatedAtUtc,
            UserInput = entry.UserInput,
            SelectedTemplateId = entry.SelectedTemplateId,
            Variables = new Dictionary<string, string>(
                entry.Variables ?? new Dictionary<string, string>(StringComparer.Ordinal),
                StringComparer.Ordinal),
            Options = new Dictionary<string, object?>(
                entry.Options ?? new Dictionary<string, object?>(StringComparer.Ordinal),
                StringComparer.Ordinal),
            GeneratedProjectPath = entry.GeneratedProjectPath,
            GeneratedZipPath = entry.GeneratedZipPath,
            Success = entry.Success,
            FailureReason = entry.FailureReason
        };
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path.Trim());
    }
}
