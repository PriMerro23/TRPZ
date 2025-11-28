using System.IO.Compression;
using ArchiverCore.Interfaces;
using ArchiverCore.Models;

namespace ArchiverCore.Adapters;

/// <summary>
/// Адаптер для архівів формату ZIP з використанням System.IO.Compression.
/// Реалізує патерни Стратегія та Адаптер.
/// </summary>
public class ZipAdapter : IArchiveStrategy
{
    public async Task CreateAsync(string archivePath, IEnumerable<string> filePaths, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var fileList = filePaths.ToList();
            int totalFiles = fileList.Count;
            int processedFiles = 0;

            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            
            foreach (var filePath in fileList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (File.Exists(filePath))
                {
                    var entryName = Path.GetFileName(filePath);
                    archive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Optimal);
                }
                else if (Directory.Exists(filePath))
                {
                    AddDirectoryToArchive(archive, filePath, Path.GetFileName(filePath));
                }

                processedFiles++;
                progress?.Report((processedFiles * 100) / totalFiles);
            }
        }, cancellationToken);
    }

    public async Task AddFilesAsync(string archivePath, IEnumerable<string> filePaths, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Використовуємо стратегію тимчасового файлу для атомарних оновлень
            var tempPath = archivePath + ".tmp";
            
            try
            {
                File.Copy(archivePath, tempPath, true);

                var fileList = filePaths.ToList();
                int totalFiles = fileList.Count;
                int processedFiles = 0;

                using (var archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
                {
                    foreach (var filePath in fileList)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (File.Exists(filePath))
                        {
                            var entryName = Path.GetFileName(filePath);
                            // Видаляємо існуючий запис, якщо присутній
                            archive.GetEntry(entryName)?.Delete();
                            archive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Optimal);
                        }
                        else if (Directory.Exists(filePath))
                        {
                            AddDirectoryToArchive(archive, filePath, Path.GetFileName(filePath));
                        }

                        processedFiles++;
                        progress?.Report((processedFiles * 100) / totalFiles);
                    }
                }

                // Атомарна заміна
                File.Delete(archivePath);
                File.Move(tempPath, archivePath);
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                throw;
            }
        }, cancellationToken);
    }

    public async Task DeleteFilesAsync(string archivePath, IEnumerable<string> fileNamesToDelete, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var tempPath = archivePath + ".tmp";
            
            try
            {
                File.Copy(archivePath, tempPath, true);

                var deleteSet = new HashSet<string>(fileNamesToDelete);
                int totalFiles = deleteSet.Count;
                int processedFiles = 0;

                using (var archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
                {
                    var entriesToDelete = archive.Entries
                        .Where(e => deleteSet.Contains(e.FullName))
                        .ToList();

                    foreach (var entry in entriesToDelete)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        entry.Delete();
                        processedFiles++;
                        progress?.Report((processedFiles * 100) / totalFiles);
                    }
                }

                File.Delete(archivePath);
                File.Move(tempPath, archivePath);
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                throw;
            }
        }, cancellationToken);
    }

    public async Task ExtractAllAsync(string archivePath, string extractPath, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(archivePath);
            int totalEntries = archive.Entries.Count;
            int processedEntries = 0;

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var destinationPath = Path.Combine(extractPath, entry.FullName);
                
                if (entry.FullName.EndsWith("/"))
                {
                    // Директорія
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    // Файл
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                processedEntries++;
                progress?.Report((processedEntries * 100) / totalEntries);
            }
        }, cancellationToken);
    }

    public async Task<IEnumerable<ArchiveEntry>> GetEntriesAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(archivePath);
            var entries = new List<ArchiveEntry>();

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                entries.Add(new ArchiveEntry
                {
                    FullName = entry.FullName,
                    CompressedSize = entry.CompressedLength,
                    UncompressedSize = entry.Length,
                    LastModified = entry.LastWriteTime.DateTime,
                    IsDirectory = entry.FullName.EndsWith("/")
                });
            }

            return entries;
        }, cancellationToken);
    }

    public async Task AcceptVisitorAsync(string archivePath, IArchiveVisitor visitor, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(archivePath);
            int totalEntries = archive.Entries.Count;
            int processedEntries = 0;

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var archiveEntry = new ArchiveEntry
                {
                    FullName = entry.FullName,
                    CompressedSize = entry.CompressedLength,
                    UncompressedSize = entry.Length,
                    LastModified = entry.LastWriteTime.DateTime,
                    IsDirectory = entry.FullName.EndsWith("/")
                };

                if (!archiveEntry.IsDirectory)
                {
                    using var stream = entry.Open();
                    visitor.Visit(archiveEntry, stream);
                }
                else
                {
                    visitor.Visit(archiveEntry, null);
                }

                processedEntries++;
                progress?.Report((processedEntries * 100) / totalEntries);
            }
        }, cancellationToken);
    }

    private void AddDirectoryToArchive(ZipArchive archive, string directoryPath, string entryBaseName)
    {
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(directoryPath, file);
            var entryName = Path.Combine(entryBaseName, relativePath).Replace("\\", "/");
            archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
        }
    }
}
