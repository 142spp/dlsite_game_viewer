using System;
using System.IO;
using System.Windows;

namespace DLGameViewer.Helpers {
    public enum ThemeType {
        Light,
        Dark,
        DeepOcean,
        CrimsonNight,
    }

    public class ThemeManager {
        private const string LightThemeSource = "Styles/LightTheme.xaml";
        private const string DarkThemeSource = "Styles/DarkTheme.xaml";
        private const string DeepOceanThemeSource = "Styles/DeepOceanTheme.xaml";
        private const string CrimsonNightThemeSource = "Styles/CrimsonNightTheme.xaml";
        private const string ThemeSettingsFileName = "themesettings.txt";
        
        // 현재 선택된 테마 유형을 추적
        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Light;
        
        // 테마 변경 이벤트
        public static event EventHandler<ThemeType>? ThemeChanged;
        
        // 테마 변경 메서드
        public static void ChangeTheme(ThemeType theme) {
            if (theme == CurrentTheme)
                return;
            
            try {
                // 이전 테마 리소스 제거
                string oldThemeSource = GetThemeSource(CurrentTheme);
                RemoveThemeResourceDictionary(oldThemeSource);
                
                // 새 테마 리소스 추가
                string newThemeSource = GetThemeSource(theme);
                ApplyThemeResourceDictionary(newThemeSource);
                
                // 현재 테마 업데이트
                CurrentTheme = theme;
                
                // 이벤트 발생
                ThemeChanged?.Invoke(null, theme);
                
                // 사용자 설정에 테마 저장
                SaveThemePreference(theme);
            }
            catch (Exception ex) {
                MessageBox.Show($"테마 변경 중 오류가 발생했습니다: {ex.Message}", "테마 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private static void RemoveThemeResourceDictionary(string source) {
            // 애플리케이션 리소스에서 테마 사전 찾기
            var resourceDict = Application.Current.Resources.MergedDictionaries;
            
            ResourceDictionary? dictionaryToRemove = null;
            foreach (var dictionary in resourceDict) {
                if (dictionary.Source != null && dictionary.Source.OriginalString.EndsWith(source)) {
                    dictionaryToRemove = dictionary;
                    break;
                }
            }
            
            if (dictionaryToRemove != null) {
                resourceDict.Remove(dictionaryToRemove);
            }
        }
        
        private static string GetThemeSource(ThemeType theme)
        {
            return theme switch
            {
                ThemeType.Light => LightThemeSource,
                ThemeType.Dark => DarkThemeSource,
                ThemeType.DeepOcean => DeepOceanThemeSource,
                ThemeType.CrimsonNight => CrimsonNightThemeSource,
                _ => LightThemeSource // Default to Light theme if unknown
            };
        }

        private static void ApplyThemeResourceDictionary(string source) {
            try {
                // 새 테마 리소스 사전 생성 및 추가
                var uri = new Uri($"/{source}", UriKind.Relative);
                var resourceDict = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);
            }
            catch (Exception ex) {
                Console.WriteLine($"테마 리소스 적용 중 오류 발생: {ex.Message}");
                throw; // 상위 메서드에서 처리하도록 예외 다시 던지기
            }
        }
        
        // 테마 설정을 저장하는 메서드
        private static void SaveThemePreference(ThemeType theme) {
            try {
                string settingsPath = GetSettingsFilePath();
                string? directoryPath = Path.GetDirectoryName(settingsPath);
                if (directoryPath != null) {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllText(settingsPath, theme.ToString());
            }
            catch (Exception ex) {
                Console.WriteLine($"테마 설정 저장 중 오류 발생: {ex.Message}");
            }
        }
        
        // 저장된 테마 설정을 불러오는 메서드
        private static ThemeType LoadThemePreference() {
            try {
                string settingsPath = GetSettingsFilePath();
                if (File.Exists(settingsPath)) {
                    string savedTheme = File.ReadAllText(settingsPath).Trim();
                    if (Enum.TryParse<ThemeType>(savedTheme, out ThemeType theme)) {
                        return theme;
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"테마 설정 불러오기 중 오류 발생: {ex.Message}");
            }
            
            return ThemeType.Light; // 기본값은 라이트 테마
        }
        
        // 설정 파일 경로를 가져오는 메서드
        private static string GetSettingsFilePath() {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRootPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            if (!Directory.Exists(Path.Combine(projectRootPath, "Data")) && Directory.Exists(baseDir)) {
                projectRootPath = baseDir; // 개발 중 VSCode 실행 경로 대응
            }
            
            return Path.Combine(projectRootPath, "Data", ThemeSettingsFileName);
        }
        
        // 초기 테마 설정
        public static void InitializeTheme() {
            try {
                // 사용자 설정에서 테마 불러오기
                ThemeType savedTheme = LoadThemePreference();
                CurrentTheme = savedTheme;
                
                // 테마 적용
                string themeSource = GetThemeSource(savedTheme);
                ApplyThemeResourceDictionary(themeSource);
            }
            catch (Exception ex) {
                // 오류 발생 시 기본 라이트 테마 적용 시도
                try {
                    CurrentTheme = ThemeType.Light;
                    ApplyThemeResourceDictionary(LightThemeSource);
                    MessageBox.Show($"테마 초기화 중 오류가 발생했습니다. 기본 테마를 적용합니다.\n오류: {ex.Message}", 
                                   "테마 초기화 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch {
                    // 기본 테마 적용마저 실패한 경우는 무시하고 애플리케이션 기본 테마 사용
                }
            }
        }
        
        // 테마 토글
        public static void ToggleTheme() {
            ThemeType newTheme;
            switch (CurrentTheme)
            {
                case ThemeType.Light:
                    newTheme = ThemeType.Dark;
                    break;
                case ThemeType.Dark:
                    newTheme = ThemeType.DeepOcean;
                    break;
                case ThemeType.DeepOcean:
                    newTheme = ThemeType.CrimsonNight;
                    break;
                case ThemeType.CrimsonNight:
                    newTheme = ThemeType.Light;
                    break;
                default:
                    newTheme = ThemeType.Light; // Default case
                    break;
            }
            ChangeTheme(newTheme);
        }
    }
} 