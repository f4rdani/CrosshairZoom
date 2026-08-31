using System;
using System.IO;
using System.Text.Json;
using CrosshairZoom.Models;

namespace CrosshairZoom.Services
{
    public class SettingsService
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairZoom");
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");
        private static readonly object _lock = new object();

        private static SettingsService? _instance;
        public static SettingsService CurrentInstance => _instance ??= new SettingsService();

        public AppSettings Current { get; private set; }

        public event Action? SettingsChanged;

        public SettingsService()
        {
            Current = new AppSettings();
            Load();
        }

        public void Load()
        {
            lock (_lock)
            {
                if (File.Exists(SettingsFilePath))
                {
                    try
                    {
                        var json = File.ReadAllText(SettingsFilePath);
                        var settings = JsonSerializer.Deserialize<AppSettings>(json);
                        if (settings != null)
                        {
                            Current = settings;
                        }
                    }
                    catch (Exception)
                    {
                        Current = new AppSettings();
                    }
                }
                else
                {
                    Current = new AppSettings();
                    Save();
                }
            }
        }

        public void Save()
        {
            lock (_lock)
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(SettingsFilePath, json);
            }

            SettingsChanged?.Invoke();
        }
    }
}
