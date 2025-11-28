namespace ArchiverCore.Interfaces;

/// <summary>
/// Інтерфейс патерну Стратегія, який визначає уніфіковані операції для всіх форматів архівів.
/// Також служить як цільовий інтерфейс для патерну Адаптер.
/// </summary>
public interface IArchiveStrategy
{
    /// <summary>
    /// Створює новий архів із вказаними файлами.
    /// </summary>
    Task CreateAsync(string archivePath, IEnumerable<string> filePaths, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Додає файли до існуючого архіву.
    /// </summary>
    Task AddFilesAsync(string archivePath, IEnumerable<string> filePaths, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Видаляє файли з існуючого архіву.
    /// </summary>
    Task DeleteFilesAsync(string archivePath, IEnumerable<string> fileNamesToDelete, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Розпаковує всі файли з архіву до вказаної директорії.
    /// </summary>
    Task ExtractAllAsync(string archivePath, string extractPath, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримує всі записи (файли) в архіві.
    /// </summary>
    Task<IEnumerable<Models.ArchiveEntry>> GetEntriesAsync(string archivePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Застосовує відвідувача до всіх записів в архіві.
    /// </summary>
    Task AcceptVisitorAsync(string archivePath, IArchiveVisitor visitor, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
