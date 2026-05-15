using System;
using System.IO;
using System.Text.Json;

namespace VidDownload.WPF.Control;

public record Settings
{
    private static readonly string SettingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "user_settings.json");

    public string Resolution { get; init; } = "1080";
    public string VideoCodec { get; init; } = "av01";
    public string AudioCodec { get; init; } = "aac";
    public string Format { get; init; } = "mp4";
    public string Theme { get; init; } = "Dark";
    public string Language { get; init; } = "ru";

    public Settings() { }

    public Settings(string resolution, string videoCodec, string audioCodec, string format)
    {
        Resolution = resolution;
        VideoCodec = videoCodec;
        AudioCodec = audioCodec;
        Format = format;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch { }
        return new Settings();
    }
}
