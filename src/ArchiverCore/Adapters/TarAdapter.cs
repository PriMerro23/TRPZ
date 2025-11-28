using System.Formats.Tar;
using System.IO.Compression;
using ArchiverCore.Interfaces;
using ArchiverCore.Models;

namespace ArchiverCore.Adapters;

/// <summary>
/// Адаптер для архівів формату TAR та TAR.GZ з використанням System.Formats.Tar.
/// Підтримує як звичайний TAR, так і TAR зі стисненням GZip.
/// </summary>
public class TarAdapter : IArchiveStrategy
{
    private readonly bool _useGzip;

    public TarAdapter(bool useGzip = false)
    {
        _useGzip = useGzip;
    }

    public async Task CreateAsync(string archivePath, IEnumerable<string> filePaths, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var fileList = filePaths.ToList();
            int totalFiles = fileList.Count;
            int processedFiles = 0;

            using (var fileStream = File.Create(archivePath))
            {
                if (_useGzip)
                {
                    using (var gzipStream = new GZipStream(fileStream, CompressionLevel.SmallestSize, leaveOpen: false))
                    using (var writer = new TarWriter(gzipStream, leaveOpen: false))
                    {
                        foreach (var filePath in fileList)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (File.Exists(filePath))
                            {
                                AddFileToTar(writer, filePath, Path.GetFileName(filePath));
                            }
                            else if (Directory.Exists(filePath))
                            {
                                AddDirectoryToTar(writer, filePath, Path.GetFileName(filePath));
                            }

                            processedFiles++;
                            progress?.Report((processedFiles * 100) / totalFiles);
                        }
                    }
                }
                else
                {
                    using (var writer = new TarWriter(fileStream, leaveOpen: false))
                    {
                        foreach (var filePath in fileList)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (File.Exists(filePath))
                            {
                                AddFileToTar(writer, filePath, Path.GetFileName(filePath));
                            }
                            else if (Directory.Exists(filePath))
                            {
                                AddDirectoryToTar(writer, filePath, Path.GetFileName(filePath));
                            }

                            processedFiles++;
                            progress?.Report((processedFiles * 100) / totalFiles);
                        }
                    }
                }
            }
        }, cancellationToken);
    }

    public async Task AddFilesAsync(string archivePath, IEnumerable<string> filePaths, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var tempPath = archivePath + ".tmp";
            
            try
            {
                // TAR не підтримує оновлення на місці, тому потрібно перестворити
                var existingEntries = new List<(TarEntry entry, byte[] data)>();
                
                // Читаємо існуючий архів
                using (var readStream = File.OpenRead(archivePath))
                {
                    // Перевірка на порожній архів
                    if (readStream.Length > 0)
                    {
                        // Автоматичне виявлення стиснення gzip
                        bool isGzipped = IsGzipCompressed(readStream);
                        readStream.Position = 0;
                        
                        Stream tarReadStream = readStream;
                        if (_useGzip || isGzipped)
                        {
                            tarReadStream = new GZipStream(readStream, CompressionMode.Decompress);
                        }

                        using (tarReadStream)
                        using (var reader = new TarReader(tarReadStream))
                        {
                            TarEntry? entry;
                            while ((entry = reader.GetNextEntry()) != null)
                            {
                                if (entry.DataStream != null)
                                {
                                    using var ms = new MemoryStream();
                                    entry.DataStream.CopyTo(ms);
                                    existingEntries.Add((entry, ms.ToArray()));
                                }
                            }
                        }
                    }
                }

                // Створюємо новий архів з існуючими + новими файлами
                using (var writeStream = File.Create(tempPath))
                {
                    if (_useGzip)
                    {
                        using (var gzipStream = new GZipStream(writeStream, CompressionLevel.SmallestSize, leaveOpen: false))
                        using (var writer = new TarWriter(gzipStream, leaveOpen: false))
                        {
                            // Записуємо існуючі записи
                            foreach (var (entry, data) in existingEntries)
                            {
                                var newEntry = new PaxTarEntry(TarEntryType.RegularFile, entry.Name)
                                {
                                    DataStream = new MemoryStream(data),
                                    ModificationTime = entry.ModificationTime
                                };
                                writer.WriteEntry(newEntry);
                            }

                            // Записуємо нові файли
                            var fileList = filePaths.ToList();
                            int totalFiles = fileList.Count;
                            int processedFiles = 0;

                            foreach (var filePath in fileList)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (File.Exists(filePath))
                                {
                                    AddFileToTar(writer, filePath, Path.GetFileName(filePath));
                                }
                                else if (Directory.Exists(filePath))
                                {
                                    AddDirectoryToTar(writer, filePath, Path.GetFileName(filePath));
                                }

                                processedFiles++;
                                progress?.Report((processedFiles * 100) / totalFiles);
                            }
                        }
                    }
                    else
                    {
                        using (var writer = new TarWriter(writeStream, leaveOpen: false))
                        {
                            // Записуємо існуючі записи
                            foreach (var (entry, data) in existingEntries)
                            {
                                var newEntry = new PaxTarEntry(TarEntryType.RegularFile, entry.Name)
                                {
                                    DataStream = new MemoryStream(data),
                                    ModificationTime = entry.ModificationTime
                                };
                                writer.WriteEntry(newEntry);
                            }

                            // Записуємо нові файли
                            var fileList = filePaths.ToList();
                            int totalFiles = fileList.Count;
                            int processedFiles = 0;

                            foreach (var filePath in fileList)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (File.Exists(filePath))
                                {
                                    AddFileToTar(writer, filePath, Path.GetFileName(filePath));
                                }
                                else if (Directory.Exists(filePath))
                                {
                                    AddDirectoryToTar(writer, filePath, Path.GetFileName(filePath));
                                }

                                processedFiles++;
                                progress?.Report((processedFiles * 100) / totalFiles);
                            }
                        }
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

    public async Task DeleteFilesAsync(string archivePath, IEnumerable<string> fileNamesToDelete, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var tempPath = archivePath + ".tmp";
            var deleteSet = new HashSet<string>(fileNamesToDelete);
            
            try
            {
                // Спочатку читаємо всі записи (не можна читати і писати одночасно)
                var entriesToKeep = new List<(TarEntry entry, byte[] data)>();
                
                // Читаємо існуючий архів та фільтруємо видалені файли
                using (var readStream = File.OpenRead(archivePath))
                {
                    // Перевірка на порожній архів
                    if (readStream.Length > 0)
                    {
                        // Auto-detect gzip compression
                        bool isGzipped = IsGzipCompressed(readStream);
                        readStream.Position = 0;
                        
                        if (_useGzip || isGzipped)
                        {
                            using var gzipStream = new GZipStream(readStream, CompressionMode.Decompress, leaveOpen: false);
                            using var reader = new TarReader(gzipStream, leaveOpen: false);
                            
                            TarEntry? entry;
                            while ((entry = reader.GetNextEntry()) != null)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (!deleteSet.Contains(entry.Name))
                                {
                                    if (entry.DataStream != null)
                                    {
                                        using var ms = new MemoryStream();
                                        entry.DataStream.CopyTo(ms);
                                        entriesToKeep.Add((entry, ms.ToArray()));
                                    }
                                }
                            }
                        }
                        else
                        {
                            using var reader = new TarReader(readStream, leaveOpen: false);
                            
                            TarEntry? entry;
                            while ((entry = reader.GetNextEntry()) != null)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (!deleteSet.Contains(entry.Name))
                                {
                                    if (entry.DataStream != null)
                                    {
                                        using var ms = new MemoryStream();
                                        entry.DataStream.CopyTo(ms);
                                        entriesToKeep.Add((entry, ms.ToArray()));
                                    }
                                }
                            }
                        }
                    }
                }

                // Тепер записуємо відфільтровані записи до нового файлу
                using (var writeStream = File.Create(tempPath))
                {
                    if (_useGzip)
                    {
                        using var gzipStream = new GZipStream(writeStream, CompressionLevel.SmallestSize, leaveOpen: false);
                        using var writer = new TarWriter(gzipStream, leaveOpen: false);
                        
                        int processed = 0;
                        foreach (var (entry, data) in entriesToKeep)
                        {
                            var newEntry = new PaxTarEntry(TarEntryType.RegularFile, entry.Name)
                            {
                                DataStream = new MemoryStream(data),
                                ModificationTime = entry.ModificationTime
                            };
                            writer.WriteEntry(newEntry);
                            
                            processed++;
                            if (deleteSet.Count > 0)
                                progress?.Report((processed * 100) / deleteSet.Count);
                        }
                    }
                    else
                    {
                        using var writer = new TarWriter(writeStream, leaveOpen: false);
                        
                        int processed = 0;
                        foreach (var (entry, data) in entriesToKeep)
                        {
                            var newEntry = new PaxTarEntry(TarEntryType.RegularFile, entry.Name)
                            {
                                DataStream = new MemoryStream(data),
                                ModificationTime = entry.ModificationTime
                            };
                            writer.WriteEntry(newEntry);
                            
                            processed++;
                            if (deleteSet.Count > 0)
                                progress?.Report((processed * 100) / deleteSet.Count);
                        }
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
            Directory.CreateDirectory(extractPath);

            using var fileStream = File.OpenRead(archivePath);
            
            // Перевірка на порожній архів
            if (fileStream.Length == 0)
            {
                return; // Порожній архів - нічого розпаковувати
            }
            
            // Auto-detect gzip compression
            bool isGzipped = IsGzipCompressed(fileStream);
            fileStream.Position = 0;
            
            Stream tarStream = fileStream;

            if (_useGzip || isGzipped)
            {
                tarStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }

            using (tarStream)
            using (var reader = new TarReader(tarStream))
            {
                int processedEntries = 0;
                TarEntry? entry;

                while ((entry = reader.GetNextEntry()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var destinationPath = Path.Combine(extractPath, entry.Name);
                    
                    if (entry.EntryType == TarEntryType.Directory)
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else if (entry.EntryType == TarEntryType.RegularFile)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }

                    processedEntries++;
                    progress?.Report(processedEntries * 10); // Приблизний прогрес
                }
            }
        }, cancellationToken);
    }

    public async Task<IEnumerable<ArchiveEntry>> GetEntriesAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var entries = new List<ArchiveEntry>();

            using var fileStream = File.OpenRead(archivePath);
            
            // Перевірка на порожній архів
            if (fileStream.Length == 0)
            {
                return entries; // Повертаємо порожній список
            }
            
            // Auto-detect gzip compression
            bool isGzipped = IsGzipCompressed(fileStream);
            fileStream.Position = 0; // Скидання позиції потоку після виявлення
            
            Stream tarStream = fileStream;

            if (_useGzip || isGzipped)
            {
                tarStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }

            using (tarStream)
            using (var reader = new TarReader(tarStream))
            {
                TarEntry? entry;
                while ((entry = reader.GetNextEntry()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    entries.Add(new ArchiveEntry
                    {
                        FullName = entry.Name,
                        CompressedSize = entry.Length,
                        UncompressedSize = entry.Length,
                        LastModified = entry.ModificationTime.DateTime,
                        IsDirectory = entry.EntryType == TarEntryType.Directory
                    });
                }
            }

            return entries;
        }, cancellationToken);
    }

    public async Task AcceptVisitorAsync(string archivePath, IArchiveVisitor visitor, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var fileStream = File.OpenRead(archivePath);
            
            // Перевірка на порожній архів
            if (fileStream.Length == 0)
            {
                return; // Порожній архів - нічого обробляти
            }
            
            // Auto-detect gzip compression
            bool isGzipped = IsGzipCompressed(fileStream);
            fileStream.Position = 0;
            
            Stream tarStream = fileStream;

            if (_useGzip || isGzipped)
            {
                tarStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }

            using (tarStream)
            using (var reader = new TarReader(tarStream))
            {
                int processedEntries = 0;
                TarEntry? entry;

                while ((entry = reader.GetNextEntry()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var archiveEntry = new ArchiveEntry
                    {
                        FullName = entry.Name,
                        CompressedSize = entry.Length,
                        UncompressedSize = entry.Length,
                        LastModified = entry.ModificationTime.DateTime,
                        IsDirectory = entry.EntryType == TarEntryType.Directory
                    };

                    visitor.Visit(archiveEntry, entry.DataStream);

                    processedEntries++;
                    progress?.Report(processedEntries * 10);
                }
            }
        }, cancellationToken);
    }

    private void AddFileToTar(TarWriter writer, string filePath, string entryName)
    {
        writer.WriteEntry(filePath, entryName);
    }

    private void AddDirectoryToTar(TarWriter writer, string directoryPath, string entryBaseName)
    {
        // Спочатку додаємо сам запис директорії (обробляє порожні директорії)
        var dirEntry = new PaxTarEntry(TarEntryType.Directory, entryBaseName + "/");
        writer.WriteEntry(dirEntry);
        
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(directoryPath, file);
            var entryName = Path.Combine(entryBaseName, relativePath).Replace("\\", "/");
            writer.WriteEntry(file, entryName);
        }
    }

    /// <summary>
    /// Перевіряє, чи є потік стиснутим за допомогою GZip, шляхом перевірки магічних байтів.
    /// </summary>
    private bool IsGzipCompressed(Stream stream)
    {
        if (stream.Length < 2)
            return false;

        var buffer = new byte[2];
        var initialPosition = stream.Position;
        
        try
        {
            int bytesRead = stream.Read(buffer, 0, 2);
            if (bytesRead < 2)
                return false;
            // Магічне число GZip - це 0x1f 0x8b
            return buffer[0] == 0x1f && buffer[1] == 0x8b;
        }
        finally
        {
            stream.Position = initialPosition;
        }
    }
}