using System;
using System.Windows;

namespace DLGameViewer.Helpers {
    public enum ThemeType {
        Light,
        Dark,
    }

    public static class ThemeManager {
        private const string LightThemeSource = "Styles/Themes/LightTheme.xaml";
        private const string DarkThemeSource = "Styles/Themes/DarkTheme.xaml";
        
        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Dark; // 기본값
        
        public static event EventHandler<ThemeType>? ThemeChanged;
        
        public static void ApplyTheme(ThemeType theme) {
            if (Application.Current == null) return;

            // 이전 테마가 있다면 제거
            string oldThemeSource = GetThemeSource(CurrentTheme);
            RemoveThemeResourceDictionary(oldThemeSource);
            
            // 새 테마 적용
            string newThemeSource = GetThemeSource(theme);
            ApplyThemeResourceDictionary(newThemeSource);
            
            CurrentTheme = theme;
            ThemeChanged?.Invoke(null, CurrentTheme);
        }
        
        public static void ToggleTheme() {
            ApplyTheme(CurrentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light);
        }

        private static string GetThemeSource(ThemeType theme)
        {
            return theme switch
            {
                ThemeType.Light => LightThemeSource,
                ThemeType.Dark => DarkThemeSource,
                _ => DarkThemeSource
            };
        }

        private static void RemoveThemeResourceDictionary(string source) {
            var resourceDicts = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary? dictionaryToRemove = null;
            foreach (var dict in resourceDicts) {
                if (dict.Source != null && dict.Source.OriginalString.EndsWith(source)) {
                    dictionaryToRemove = dict;
                    break;
                }
            }
            
            if (dictionaryToRemove != null) {
                resourceDicts.Remove(dictionaryToRemove);
            }
        }

        private static void ApplyThemeResourceDictionary(string source) {
            try {
                var uri = new Uri(source, UriKind.Relative);
                var resourceDict = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error applying theme resource: {ex.Message}");
            }
        }
    }
} 