using ArchiverCore.Interfaces;
using ArchiverCore.Models;

namespace ArchiverCore.Visitors;

/// <summary>
/// Відвідувач, який перевіряє цілісність архіву, намагаючись прочитати всі записи.
/// Реалізує патерн Відвідувач для валідації архіву.
/// </summary>
public class TestVisitor : IArchiveVisitor
{
    private readonly TestResult _result = new() { IsValid = true };

    public void Visit(ArchiveEntry entry, Stream? contentStream)
    {
        try
        {
            if (!entry.IsDirectory && contentStream != null)
            {
                // Намагаємося прочитати потік, щоб перевірити, що він не пошкоджений
                var buffer = new byte[8192];
                while (contentStream.Read(buffer, 0, buffer.Length) > 0)
                {
                    // Просто читаємо для перевірки
                }
            }

            _result.EntriesChecked++;
        }
        catch (Exception ex)
        {
            _result.IsValid = false;
            _result.Errors.Add($"Error testing '{entry.FullName}': {ex.Message}");
        }
    }

    public object GetResult()
    {
        return _result;
    }
}
