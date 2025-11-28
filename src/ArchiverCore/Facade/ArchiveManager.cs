using ArchiverCore.Factories;
using ArchiverCore.Models;
using ArchiverCore.Services;
using ArchiverCore.Visitors;
using ArchiverCore.Interfaces;

namespace ArchiverCore.Facade;

/// <summary>
/// Патерн Фасад - надає уніфікований високорівневий інтерфейс для всіх операцій з архівами.
/// Координує Фабрику, Адаптери, Відвідувачів та операції з базою даних.
/// </summary>
public class ArchiveManager
{
    private readonly IArchiveRepository? _repository;

    public ArchiveManager(IArchiveRepository? repository = null)
    {
        _repository = repository;
    }

    /// <summary>
    /// Створює новий архів із вказаними файлами.
    /// </summary>
    public async Task<string> CreateArchiveAsync(
        string archivePath, 
        IEnumerable<string> filePaths, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var adapter = ArchiveFactory.CreateFor(archivePath);
        
        await adapter.CreateAsync(archivePath, filePaths, progress, cancellationToken);

        // Обчислюємо контрольну суму
        using var checksumVisitor = new ChecksumVisitor();
        await adapter.AcceptVisitorAsync(archivePath, checksumVisitor, cancellationToken: cancellationToken);
        var checksum = (string)checksumVisitor.GetResult();

        // Зберігаємо метадані в базу даних
        await UpdateArchiveMetadataAsync(archivePath, checksum, adapter, cancellationToken);
        await LogOperationAsync(archivePath, "CREATE", "Success", $"Checksum: {checksum}");

        return checksum;
    }

    /// <summary>
    /// Додає файли до існуючого архіву.
    /// </summary>
    public async Task<string> AddFilesToArchiveAsync(
        string archivePath, 
        IEnumerable<string> filePaths, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive not found", archivePath);

        var adapter = ArchiveFactory.CreateFor(archivePath);
        
        await adapter.AddFilesAsync(archivePath, filePaths, progress, cancellationToken);

        // Перераховуємо контрольну суму
        using var checksumVisitor = new ChecksumVisitor();
        await adapter.AcceptVisitorAsync(archivePath, checksumVisitor, cancellationToken: cancellationToken);
        var checksum = (string)checksumVisitor.GetResult();

        // Оновлюємо метадані в базі даних
        await UpdateArchiveMetadataAsync(archivePath, checksum, adapter, cancellationToken);
        await LogOperationAsync(archivePath, "ADD_FILES", "Success", $"Added {filePaths.Count()} files. New checksum: {checksum}");

        return checksum;
    }

    /// <summary>
    /// Видаляє файли з існуючого архіву.
    /// </summary>
    public async Task<string> DeleteFromArchiveAsync(
        string archivePath, 
        IEnumerable<string> fileNames, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive not found", archivePath);

        var adapter = ArchiveFactory.CreateFor(archivePath);
        
        await adapter.DeleteFilesAsync(archivePath, fileNames, progress, cancellationToken);

        // Перераховуємо контрольну суму
        using var checksumVisitor = new ChecksumVisitor();
        await adapter.AcceptVisitorAsync(archivePath, checksumVisitor, cancellationToken: cancellationToken);
        var checksum = (string)checksumVisitor.GetResult();

        // Оновлюємо метадані в базі даних
        await UpdateArchiveMetadataAsync(archivePath, checksum, adapter, cancellationToken);
        await LogOperationAsync(archivePath, "DELETE_FILES", "Success", $"Deleted {fileNames.Count()} files. New checksum: {checksum}");

        return checksum;
    }

    /// <summary>
    /// Перевіряє цілісність архіву.
    /// </summary>
    public async Task<TestResult> TestArchiveAsync(
        string archivePath, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive not found", archivePath);

        var adapter = ArchiveFactory.CreateFor(archivePath);
        var testVisitor = new TestVisitor();
        
        await adapter.AcceptVisitorAsync(archivePath, testVisitor, progress, cancellationToken);
        
        var result = (TestResult)testVisitor.GetResult();

        // Логуємо в базу даних
        await LogOperationAsync(archivePath, "TEST", result.IsValid ? "Success" : "Failed", 
            $"Checked {result.EntriesChecked} entries. Valid: {result.IsValid}");

        return result;
    }

    /// <summary>
    /// Отримує список записів в архіві.
    /// </summary>
    public async Task<IEnumerable<ArchiveEntry>> GetArchiveEntriesAsync(
        string archivePath, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive not found", archivePath);

        var adapter = ArchiveFactory.CreateFor(archivePath);
        return await adapter.GetEntriesAsync(archivePath, cancellationToken);
    }

    /// <summary>
    /// Розпаковує всі файли з архіву.
    /// </summary>
    public async Task ExtractArchiveAsync(
        string archivePath, 
        string extractPath, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive not found", archivePath);

        var adapter = ArchiveFactory.CreateFor(archivePath);
        await adapter.ExtractAllAsync(archivePath, extractPath, progress, cancellationToken);

        await LogOperationAsync(archivePath, "EXTRACT", "Success", $"Extracted to: {extractPath}");
    }

    /// <summary>
    /// Розділяє архів на кілька томів.
    /// </summary>
    public async Task<List<string>> SplitArchiveAsync(
        string archivePath, 
        long volumeSizeBytes, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var splitter = new VolumeSplitter();
        var volumes = await splitter.SplitAsync(archivePath, volumeSizeBytes, progress, cancellationToken);

        await LogOperationAsync(archivePath, "SPLIT", "Success", 
            $"Created {volumes.Count} volumes of {volumeSizeBytes} bytes");

        return volumes;
    }

    /// <summary>
    /// Об'єднує файли томів у єдиний архів.
    /// </summary>
    public async Task MergeArchiveAsync(
        string firstVolumePath, 
        string? outputPath = null, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var splitter = new VolumeSplitter();
        await splitter.MergeAsync(firstVolumePath, outputPath, progress, cancellationToken);

        await LogOperationAsync(outputPath ?? firstVolumePath.Replace(".part001", ""), 
            "MERGE", "Success", $"Merged from: {firstVolumePath}");
    }

    /// <summary>
    /// Обчислює контрольну суму для архіву.
    /// </summary>
    public async Task<string> CalculateChecksumAsync(
        string archivePath, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive not found", archivePath);

        var adapter = ArchiveFactory.CreateFor(archivePath);
        using var checksumVisitor = new ChecksumVisitor();
        await adapter.AcceptVisitorAsync(archivePath, checksumVisitor, cancellationToken: cancellationToken);
        
        return (string)checksumVisitor.GetResult();
    }

    private async Task UpdateArchiveMetadataAsync(
        string archivePath, 
        string checksum, 
        Interfaces.IArchiveStrategy adapter, 
        CancellationToken cancellationToken)
    {
        if (_repository == null)
        {
            System.Diagnostics.Debug.WriteLine("WARNING: Repository is null, skipping metadata update");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"Updating metadata for: {archivePath}");
            
            // Оновлюємо контрольну суму
            await _repository.UpdateChecksumAsync(archivePath, checksum);
            System.Diagnostics.Debug.WriteLine($"Checksum updated: {checksum}");

            // Синхронізуємо записи
            var entries = await adapter.GetEntriesAsync(archivePath, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"Got {entries.Count()} entries from archive");
            
            await _repository.SyncEntriesAsync(archivePath, entries);
            System.Diagnostics.Debug.WriteLine("Entries synced successfully");
        }
        catch (Exception ex)
        {
            // Логуємо повну помилку
            System.Diagnostics.Debug.WriteLine($"ERROR updating metadata: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            throw; // Повторно викидаємо помилку для відображення в UI
        }
    }

    private async Task DeleteArchiveFromDatabaseAsync(string archivePath)
    {
        if (_repository == null)
            return;

        try
        {
            await _repository.DeleteArchiveAsync(archivePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting archive from database: {ex.Message}");
        }
    }

    private async Task LogOperationAsync(string archivePath, string operation, string result, string metadata)
    {
        if (_repository == null)
            return;

        try
        {
            await _repository.LogOperationAsync(archivePath, operation, result, metadata);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error logging operation: {ex.Message}");
        }
    }
}
