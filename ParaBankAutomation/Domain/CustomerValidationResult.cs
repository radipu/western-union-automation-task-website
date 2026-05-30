namespace ParaBankAutomation.Models;

public sealed class CustomerValidationResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<string> Warnings => _warnings;
    public bool IsValid => _errors.Count == 0;
    public bool HasWarnings => _warnings.Count > 0;

    public void AddError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message)) _errors.Add(message.Trim());
    }

    public void AddWarning(string message)
    {
        if (!string.IsNullOrWhiteSpace(message)) _warnings.Add(message.Trim());
    }

    public string ToNoteText()
    {
        var parts = new List<string>();
        if (_errors.Count > 0) parts.Add("Validation errors: " + string.Join("; ", _errors));
        if (_warnings.Count > 0) parts.Add("Validation warnings: " + string.Join("; ", _warnings));
        return string.Join(" | ", parts);
    }
}
