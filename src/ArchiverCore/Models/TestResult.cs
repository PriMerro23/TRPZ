namespace ArchiverCore.Models;

/// <summary>
/// Представляє результат операції тестування архіву.
/// </summary>
public class TestResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int EntriesChecked { get; set; }

    public override string ToString()
    {
        if (IsValid)
            return $"Archive is valid. {EntriesChecked} entries checked.";
        else
            return $"Archive has errors:\n" + string.Join("\n", Errors);
    }
}
