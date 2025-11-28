namespace ArchiverCore.Services;

/// <summary>
/// Сервіс для розділення архівів на томи та їх об'єднання.
/// </summary>
public class VolumeSplitter
{
    /// <summary>
    /// Розділяє архів на кілька томів вказаного розміру.
    /// </summary>
    /// <param name="archivePath">Шлях до вихідного архіву</param>
    /// <param name="volumeSizeBytes">Розмір кожного тому в байтах</param>
    /// <param name="progress">Звітувач прогресу</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>Список шляхів створених файлів томів</returns>
    public async Task<List<string>> SplitAsync(
        string archivePath, 
        long volumeSizeBytes, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive file not found", archivePath);

        if (volumeSizeBytes <= 0)
            throw new ArgumentException("Volume size must be positive", nameof(volumeSizeBytes));

        var volumePaths = new List<string>();
        var baseDirectory = Path.GetDirectoryName(archivePath) ?? "";
        var baseFileName = Path.GetFileNameWithoutExtension(archivePath);
        var extension = Path.GetExtension(archivePath);

        return await Task.Run(() =>
        {
            using var sourceStream = File.OpenRead(archivePath);
            long totalSize = sourceStream.Length;
            long bytesRead = 0;
            int volumeNumber = 1;
            var buffer = new byte[81920]; // 80KB buffer

            while (bytesRead < totalSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var volumePath = Path.Combine(baseDirectory, 
                    $"{baseFileName}{extension}.part{volumeNumber:D3}");
                volumePaths.Add(volumePath);

                using (var volumeStream = File.Create(volumePath))
                {
                    long volumeBytesWritten = 0;

                    while (volumeBytesWritten < volumeSizeBytes && bytesRead < totalSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int bytesToRead = (int)Math.Min(buffer.Length, 
                            Math.Min(volumeSizeBytes - volumeBytesWritten, totalSize - bytesRead));
                        
                        int read = sourceStream.Read(buffer, 0, bytesToRead);
                        if (read == 0) break;

                        volumeStream.Write(buffer, 0, read);
                        volumeBytesWritten += read;
                        bytesRead += read;

                        progress?.Report((int)((bytesRead * 100) / totalSize));
                    }
                }

                volumeNumber++;
            }

            return volumePaths;
        }, cancellationToken);
    }

    /// <summary>
    /// Об'єднує файли томів у єдиний архів.
    /// </summary>
    /// <param name="firstVolumePath">Шлях до першого тому (.part001)</param>
    /// <param name="outputPath">Шлях для об'єднаного вихідного файлу (якщо null, використовується оригінальне ім'я)</param>
    /// <param name="progress">Звітувач прогресу</param>
    /// <param name="cancellationToken">Токен скасування</param>
    public async Task MergeAsync(
        string firstVolumePath, 
        string? outputPath = null, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(firstVolumePath))
            throw new FileNotFoundException("First volume file not found", firstVolumePath);

        // Визначаємо вихідний шлях
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = firstVolumePath.Replace(".part001", "");
        }

        // Знаходимо всі файли томів
        var volumePaths = FindVolumeParts(firstVolumePath);
        
        if (volumePaths.Count == 0)
            throw new InvalidOperationException("No volume parts found");

        await Task.Run(() =>
        {
            long totalSize = volumePaths.Sum(p => new FileInfo(p).Length);
            long bytesWritten = 0;
            var buffer = new byte[81920];

            using var outputStream = File.Create(outputPath);

            foreach (var volumePath in volumePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var volumeStream = File.OpenRead(volumePath);
                
                int read;
                while ((read = volumeStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    outputStream.Write(buffer, 0, read);
                    bytesWritten += read;

                    progress?.Report((int)((bytesWritten * 100) / totalSize));
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Знаходить всі частини томів для вказаного першого тому.
    /// </summary>
    private List<string> FindVolumeParts(string firstVolumePath)
    {
        var directory = Path.GetDirectoryName(firstVolumePath) ?? "";
        var fileName = Path.GetFileName(firstVolumePath);
        
        // Витягуємо базове ім'я, видаляючи .part001
        var baseName = fileName.Replace(".part001", "");
        
        var volumePaths = new List<string>();
        int volumeNumber = 1;

        while (true)
        {
            var volumePath = Path.Combine(directory, $"{baseName}.part{volumeNumber:D3}");
            
            if (!File.Exists(volumePath))
                break;

            volumePaths.Add(volumePath);
            volumeNumber++;
        }

        return volumePaths;
    }

    /// <summary>
    /// Отримує інформацію про частини томів.
    /// </summary>
    public VolumeInfo GetVolumeInfo(string firstVolumePath)
    {
        var parts = FindVolumeParts(firstVolumePath);
        
        return new VolumeInfo
        {
            PartCount = parts.Count,
            TotalSize = parts.Sum(p => new FileInfo(p).Length),
            Parts = parts
        };
    }
}

/// <summary>
/// Інформація про томи архіву.
/// </summary>
public class VolumeInfo
{
    public int PartCount { get; set; }
    public long TotalSize { get; set; }
    public List<string> Parts { get; set; } = new();
}
