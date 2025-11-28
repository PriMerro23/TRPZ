using System.Security.Cryptography;
using System.Text;
using ArchiverCore.Interfaces;
using ArchiverCore.Models;

namespace ArchiverCore.Visitors;

/// <summary>
/// Відвідувач, який обчислює контрольну суму SHA256 вмісту архіву.
/// Реалізує патерн Відвідувач для перевірки цілісності архіву.
/// </summary>
public class ChecksumVisitor : IArchiveVisitor, IDisposable
{
    private readonly SHA256 _sha256 = SHA256.Create();
    private readonly MemoryStream _combinedStream = new();

    public void Visit(ArchiveEntry entry, Stream? contentStream)
    {
        if (entry.IsDirectory || contentStream == null)
            return;

        // Додаємо ім'я запису до контрольної суми
        var nameBytes = Encoding.UTF8.GetBytes(entry.FullName);
        _combinedStream.Write(nameBytes);

        // Додаємо вміст до контрольної суми
        contentStream.CopyTo(_combinedStream);
    }

    public object GetResult()
    {
        _combinedStream.Position = 0;
        var hash = _sha256.ComputeHash(_combinedStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public void Dispose()
    {
        _sha256?.Dispose();
        _combinedStream?.Dispose();
    }
}
