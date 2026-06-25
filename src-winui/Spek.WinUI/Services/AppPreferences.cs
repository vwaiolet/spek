using System.Text.Json;

namespace Spek.WinUI.Services;

public sealed class AppPreferences
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public bool CheckForUpdates { get; set; } = true;
    public int LastUpdateDay { get; set; }
    public string Language { get; set; } = "";

    public static AppPreferences Load()
    {
        string path = PreferencesPath;
        if (!File.Exists(path))
        {
            return new AppPreferences();
        }

        try
        {
            return JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(path)) ?? new AppPreferences();
        }
        catch
        {
            return new AppPreferences();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
        File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    private static string PreferencesPath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Spek", "winui-settings.json");
        }
    }
}
