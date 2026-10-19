// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBKeybinds;

public sealed class AppSettings
{
    public string? InputIniPath { get; set; }
    public uint HotkeyModifiers { get; set; }
    public string HotkeyKey { get; set; } = "f10";
    public string LastCategory { get; set; } = "move";
    public int WindowWidth { get; set; } = 1020;
    public int WindowHeight { get; set; } = 780;
    public bool FirstRunTipShown { get; set; }

    public static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BB按键设置");

    public static string FilePath => Path.Combine(Dir, "settings.json");
    public static string LogPath => Path.Combine(Dir, "bbkeybinds.log");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions);
                if (s != null)
                    return s;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write("读取设置失败: " + ex.Message);
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex)
        {
            AppLog.Write("保存设置失败: " + ex.Message);
        }
    }

    [JsonIgnore]
    public HotkeySpec Hotkey => new(HotkeyModifiers, HotkeyKey);
}

public static class AppLog
{
    private const long MaxBytes = 256 * 1024;

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.Dir);
            var fi = new FileInfo(AppSettings.LogPath);
            if (fi.Exists && fi.Length > MaxBytes)
            {
                var old = AppSettings.LogPath + ".old";
                if (File.Exists(old)) File.Delete(old);
                File.Move(AppSettings.LogPath, old);
            }
            File.AppendAllText(AppSettings.LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n");
        }
        catch { }
    }
}
