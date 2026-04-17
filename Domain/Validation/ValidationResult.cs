namespace FolderAssi.Domain.Validation;

public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = [];

    public IReadOnlyList<ValidationError> Errors => _errors;

    public bool IsValid => _errors.Count == 0;

    public void AddError(string code, string message, string? path = null)
    {
        _errors.Add(new ValidationError
        {
            Code = code,
            Message = message,
            Path = path,
        });
    }

    public void Add(string code, string message, string? path = null)
    {
        AddError(code, message, path);
    }

    public void Add(ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _errors.Add(error);
    }

    public void Merge(ValidationResult other)
    {
        ArgumentNullException.ThrowIfNull(other);
        _errors.AddRange(other.Errors);
    }

    public static ValidationResult Success() => new();
}
