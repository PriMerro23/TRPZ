using ArchiverCore.Interfaces;

namespace ArchiverCore.Factories;

/// <summary>
/// Реалізація патерну Фабричний метод.
/// Створює відповідний адаптер на основі розширення файлу архіву.
/// </summary>
public class ArchiveFactory
{
    /// <summary>
    /// Створює відповідну реалізацію IArchiveStrategy на основі розширення файлу.
    /// </summary>
    public static IArchiveStrategy CreateFor(string archivePath)
    {
        var extension = Path.GetExtension(archivePath).ToLowerInvariant();
        
        return extension switch
        {
            ".zip" => new Adapters.ZipAdapter(),
            ".tar" => new Adapters.TarAdapter(useGzip: false),
            ".gz" when archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) 
                => new Adapters.TarAdapter(useGzip: true),
            _ => throw new NotSupportedException($"Archive format '{extension}' is not supported. Supported formats: .zip, .tar, .tar.gz")
        };
    }
 
    /// <summary>
    /// Перевіряє, чи підтримується розширення файлу.
    /// </summary>
    public static bool IsSupported(string archivePath)
    {
        var extension = Path.GetExtension(archivePath).ToLowerInvariant();
        return extension == ".zip" 
            || extension == ".tar" 
            || archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Отримує всі підтримувані розширення.
    /// </summary>
    public static string[] GetSupportedExtensions()
    {
        return new[] { ".zip", ".tar", ".tar.gz" };
    }
}
