using System;
using System.IO;
using System.Text.Json;

namespace TokenMonitorApp.Services
{
    public static class SettingsStore
    {
        private static readonly string AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TokenMonitorApp");
        private static readonly string SettingsFile = Path.Combine(AppDir, "settings.json");

        public static FilterSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<FilterSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (settings != null) return settings;
                }
            }
            catch { }
            return new FilterSettings();
        }

        public static void Save(FilterSettings settings)
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
    }
}
