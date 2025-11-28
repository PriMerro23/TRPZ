using Microsoft.EntityFrameworkCore;
using ArchiverInfrastructure.Data;
using ArchiverInfrastructure.Entities;
using ArchiverCore.Models;
using ArchiverCore.Interfaces;

namespace ArchiverInfrastructure.Repositories;

/// <summary>
/// Репозиторій для операцій з базою даних, пов'язаних з архівами, записами та операціями.
/// </summary>
public class ArchiveRepository : IArchiveRepository
{
    private readonly ArchiverDbContext _context;

    public ArchiveRepository(ArchiverDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Логує операцію в базу даних.
    /// </summary>
    public async Task LogOperationAsync(string archivePath, string operationType, string result, string? metadata = null)
    {
        var archive = await GetOrCreateArchiveAsync(archivePath);

        var operation = new Operation
        {
            ArchiveId = archive.Id,
            OperationType = operationType,
            Result = result,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow
        };

        _context.Operations.Add(operation);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Синхронізує записи в базі даних з фактичним вмістом архіву.
    /// </summary>
    public async Task SyncEntriesAsync(string archivePath, IEnumerable<ArchiveEntry> entries)
    {
        System.Diagnostics.Debug.WriteLine($"SyncEntriesAsync called for: {archivePath}");
        var archive = await GetOrCreateArchiveAsync(archivePath);
        System.Diagnostics.Debug.WriteLine($"Archive ID: {archive.Id}, Path: {archive.FilePath}");

        // Видаляємо існуючі записи
        var existingEntries = await _context.Entries
            .Where(e => e.ArchiveId == archive.Id)
            .ToListAsync();
        
        System.Diagnostics.Debug.WriteLine($"Removing {existingEntries.Count} existing entries");
        _context.Entries.RemoveRange(existingEntries);

        // Додаємо нові записи
        int addedCount = 0;
        foreach (var entry in entries)
        {
            // PostgreSQL requires UTC timestamps, convert if needed
            var modifiedDate = entry.LastModified.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(entry.LastModified, DateTimeKind.Utc)
                : entry.LastModified.ToUniversalTime();

            _context.Entries.Add(new Entry
            {
                ArchiveId = archive.Id,
                FileName = entry.FullName,
                FileSize = entry.UncompressedSize,
                ModifiedDate = modifiedDate,
                IsDirectory = entry.IsDirectory
            });
            addedCount++;
        }
        
        System.Diagnostics.Debug.WriteLine($"Added {addedCount} new entries");

        archive.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        System.Diagnostics.Debug.WriteLine("SaveChanges completed successfully");
    }

    /// <summary>
    /// Оновлює контрольну суму архіву в базі даних.
    /// </summary>
    public async Task UpdateChecksumAsync(string archivePath, string checksum)
    {
        var archive = await GetOrCreateArchiveAsync(archivePath);
        archive.Checksum = checksum;
        archive.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Отримує метадані архіву за шляхом.
    /// </summary>
    public async Task<Archive?> GetArchiveByPathAsync(string archivePath)
    {
        return await _context.Archives
            .Include(a => a.Entries)
            .Include(a => a.Operations)
            .FirstOrDefaultAsync(a => a.FilePath == archivePath);
    }

    /// <summary>
    /// Отримує останні операції для архіву.
    /// </summary>
    public async Task<List<Operation>> GetRecentOperationsAsync(string? archivePath = null, int count = 50)
    {
        var query = _context.Operations
            .Include(o => o.Archive)
            .OrderByDescending(o => o.Timestamp)
            .AsQueryable();

        if (!string.IsNullOrEmpty(archivePath))
        {
            query = query.Where(o => o.Archive != null && o.Archive.FilePath == archivePath);
        }

        return await query.Take(count).ToListAsync();
    }

    /// <summary>
    /// Отримує або створює запис архіву в базі даних.
    /// </summary>
    private async Task<Archive> GetOrCreateArchiveAsync(string archivePath)
    {
        var archive = await _context.Archives
            .FirstOrDefaultAsync(a => a.FilePath == archivePath);

        if (archive == null)
        {
            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            var archiveType = extension switch
            {
                ".zip" => "ZIP",
                ".tar" => "TAR",
                ".gz" => "TAR.GZ",
                _ => "UNKNOWN"
            };

            archive = new Archive
            {
                FilePath = archivePath,
                ArchiveType = archiveType,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _context.Archives.Add(archive);
            await _context.SaveChangesAsync();
        }

        return archive;
    }

    /// <summary>
    /// Видаляє архів та всі пов'язані дані з бази даних.
    /// </summary>
    public async Task DeleteArchiveAsync(string archivePath)
    {
        var archive = await _context.Archives
            .FirstOrDefaultAsync(a => a.FilePath == archivePath);

        if (archive != null)
        {
            _context.Archives.Remove(archive);
            await _context.SaveChangesAsync();
        }
    }
}
