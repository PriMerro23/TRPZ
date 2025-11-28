namespace ArchiverCore.Models;

/// <summary>
/// Представляє запис файлу або директорії в архіві.
/// </summary>
public class ArchiveEntry
{
    public string FullName { get; set; } = string.Empty;
    public long CompressedSize { get; set; }
    public long UncompressedSize { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDirectory { get; set; }

    public override string ToString()
    {
        return $"{FullName} ({(IsDirectory ? "DIR" : $"{UncompressedSize} bytes")})";
    }
}
