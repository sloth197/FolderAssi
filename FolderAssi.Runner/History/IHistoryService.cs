internal interface IHistoryService
{
    IReadOnlyList<GenerationHistoryEntry> GetRecent(int limit = 20);
    void Add(GenerationHistoryEntry entry);
    string HistoryFilePath { get; }
}
