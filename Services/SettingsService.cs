using System;
using System.IO;
using System.Text.Json;
using DLGameViewer.Interfaces;
using DLGameViewer.Models;

namespace DLGameViewer.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "DLGameViewer");
            Directory.CreateDirectory(appFolderPath);
            _settingsFilePath = Path.Combine(appFolderPath, "settings.json");

            Settings = new AppSettings();
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Settings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                // 설정 로드 실패 시 기본값 사용
                Console.WriteLine($"Failed to load settings: {ex.Message}");
                Settings = new AppSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
