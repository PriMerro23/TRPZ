using System.Text.Json;

namespace ArchiverApp;

/// <summary>
/// Налаштування конфігурації додатку.
/// </summary>
public class AppSettings
{
    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=archiver_db;Username=postgres;Password=postgres";
    public string LogDirectory { get; set; } = "logs";
    public int DefaultVolumeSizeMB { get; set; } = 100;

    private static readonly string ConfigPath = "appsettings.json";

    public static AppSettings Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        var defaultSettings = new AppSettings();
        defaultSettings.Save();
        return defaultSettings;
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(ConfigPath, json);
    }
}
