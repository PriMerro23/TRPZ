using ArchiverCore.Models;

namespace ArchiverCore.Interfaces;

/// <summary>
/// Інтерфейс для репозиторія операцій з базою даних архівів.
/// Дозволяє ArchiveManager зберігати метадані без прямої залежності від інфраструктурного шару.
/// </summary>
public interface IArchiveRepository
{
    /// <summary>
    /// Оновлює контрольну суму архіву в базі даних.
    /// </summary>
    Task UpdateChecksumAsync(string archivePath, string checksum);

    /// <summary>
    /// Синхронізує записи в базі даних з фактичним вмістом архіву.
    /// </summary>
    Task SyncEntriesAsync(string archivePath, IEnumerable<ArchiveEntry> entries);

    /// <summary>
    /// Логує операцію в базу даних.
    /// </summary>
    Task LogOperationAsync(string archivePath, string operationType, string result, string? metadata = null);

    /// <summary>
    /// Видаляє архів та всі пов'язані дані з бази даних.
    /// </summary>
    Task DeleteArchiveAsync(string archivePath);
}
