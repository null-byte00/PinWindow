using System.IO;
using System.Text.Json;

namespace PinWindow;

public sealed class Settings
{
    public string PinHotkey { get; set; } = "Ctrl+Alt+P";
    public string GhostHotkey { get; set; } = "Ctrl+Alt+G";  // сквозной клик у закреплённого
    public bool StartWithWindows { get; set; }

    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PinWindow");
    static readonly string File = Path.Combine(Dir, "settings.json");

    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllText(File)) ?? new(); }
        catch { return new(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
