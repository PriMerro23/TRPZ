namespace ArchiverInfrastructure.Logging;

/// <summary>
/// Простий файловий логер.
/// </summary>
public class FileLogger
{
    private readonly string _logDirectory;
    private static readonly object _lock = new();

    public FileLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public void Log(string level, string message, Exception? exception = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logMessage = $"[{timestamp}] [{level}] {message}";
        
        if (exception != null)
        {
            logMessage += $"\nException: {exception.Message}\nStackTrace: {exception.StackTrace}";
        }

        var logFile = Path.Combine(_logDirectory, $"archiver-{DateTime.Now:yyyy-MM-dd}.log");

        lock (_lock)
        {
            File.AppendAllText(logFile, logMessage + Environment.NewLine);
        }
    }

    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARNING", message);
    public void Error(string message, Exception? exception = null) => Log("ERROR", message, exception);
    public void Debug(string message) => Log("DEBUG", message);
}
