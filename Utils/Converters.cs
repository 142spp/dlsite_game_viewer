using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Linq;
using DLGameViewer.Helpers;

namespace DLGameViewer.Utils {
    // 로컬 경로를 이미지 소스로 변환하는 컨버터
    public class PathToImageSourceConverter : IValueConverter {
        // 기본 디코딩 너비와 상대적 비율 설정 - 메인 윈도우 용 (1600x1200)
        private const int MainWindowListImageWidth = 200;
        
        // 게임 정보 다이얼로그 용 (1200x800)
        private const int DialogMainImageWidth = 560;
        private const int DialogPreviewImageWidth = 200;
        
        // 기준 윈도우 크기
        private const double MainWindowWidth = 1600.0;
        private const double MainWindowHeight = 1200.0;
        private const double DialogWindowWidth = 1200.0;
        private const double DialogWindowHeight = 800.0;
        
        // 화면 크기 변경 감지를 위한 필드
        private static double lastWindowWidth = 0;
        private static double lastWindowHeight = 0;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string localPath = value as string ?? string.Empty;
            string parameterString = parameter as string ?? string.Empty;

            if (string.IsNullOrWhiteSpace(localPath)) {
                return null;
            }

            try {
                // 현재 활성화된 윈도우 확인
                Window currentWindow = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
                
                // 현재 윈도우 크기 가져오기
                double windowWidth = currentWindow?.ActualWidth ?? 0;
                double windowHeight = currentWindow?.ActualHeight ?? 0;
                
                double baseWidth = 0;
                double baseHeight = 0;

                // 이미지 유형에 따라 적절한 기준 및 크기 설정  
                if (parameterString == "MainImageContent" || parameterString == "PreviewImageContent") {
                    baseWidth = DialogWindowWidth;
                    baseHeight = DialogWindowHeight;
                } else {
                    baseWidth = MainWindowWidth;
                    baseHeight = MainWindowHeight;
                }
                
                // 윈도우가 실제로 렌더링된 경우에만 크기 비율 계산
                double scaleRatio = 1.0;
                if (windowWidth > 0 && windowHeight > 0) {
                    double widthRatio = windowWidth / baseWidth;
                    double heightRatio = windowHeight / baseHeight;
                    scaleRatio = Math.Min(widthRatio, heightRatio);
                }
                
                // 이미지 유형에 따라 적절한 크기 선택
                int targetWidth;
                if (parameterString == "ListImageContent") {
                    targetWidth = (int)(MainWindowListImageWidth * scaleRatio);
                } else if (parameterString == "MainImageContent") {
                    targetWidth = (int)(DialogMainImageWidth * scaleRatio);
                } else if (parameterString == "PreviewImageContent") {
                    targetWidth = (int)(DialogPreviewImageWidth * scaleRatio);
                } else {
                    targetWidth = (int)(DialogMainImageWidth * scaleRatio);
                }
                
                // 최소 크기 보장
                targetWidth = Math.Max(targetWidth, 100);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(localPath, UriKind.Absolute);
                bitmap.DecodePixelWidth = targetWidth;
                bitmap.CacheOption = BitmapCacheOption.OnLoad; 
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // 캐시 문제 방지
                bitmap.EndInit();
                bitmap.Freeze(); // UI 스레드 외 접근 및 성능 향상
                
                // 윈도우 크기 갱신
                lastWindowWidth = windowWidth;
                lastWindowHeight = windowHeight;
                
                return bitmap;
            } catch (Exception ex) {
                // 이미지 로딩 실패 (잘못된 형식, 손상된 파일 등)
                Console.WriteLine($"Error loading image from {localPath}: {ex.Message}");
                return null;
            }
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    // ThemeType을 bool 값으로 변환하는 컨버터 (다크 모드일 때 true, 라이트 모드일 때 false)
    public class ThemeTypeConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is ThemeType themeType) {
                return themeType == ThemeType.Dark;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool isDarkMode) {
                return isDarkMode ? ThemeType.Dark : ThemeType.Light;
            }
            return ThemeType.Light;
        }
    }
    
    // Boolean 값을 Visibility로 변환하는 컨버터
    public class BooleanToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool visible = false;
            
            if (value is bool boolValue) {
                visible = boolValue;
            } else if (value is string stringValue) {
                visible = !string.IsNullOrEmpty(stringValue);
            } else if (value != null) {
                visible = true;
            }
            
            // 파라미터가 "Inverse"인 경우 결과를 반전
            if (parameter is string strParam && strParam == "Inverse") {
                visible = !visible;
            }
            
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Visibility visibility) {
                bool result = visibility == Visibility.Visible;
                
                // 파라미터가 "Inverse"인 경우 결과를 반전
                if (parameter is string strParam && strParam == "Inverse") {
                    result = !result;
                }
                
                return result;
            }
            
            return false;
        }
    }

    // 문자열이 비어있지 않은지 확인하는 컨버터
    public class StringNotEmptyToBoolConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string stringValue) {
                return !string.IsNullOrEmpty(stringValue);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    
    // Boolean 값을 지정된 텍스트로 변환하는 컨버터 (오름차순/내림차순 등)
    public class BooleanToTextConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is bool boolValue)) {
                return string.Empty;
            }
            
            // 파라미터 형식: "TrueText:FalseText"
            if (parameter is string parameterString) {
                string[] textOptions = parameterString.Split(':');
                if (textOptions.Length == 2) {
                    return boolValue ? textOptions[0] : textOptions[1];
                }
            }
            
            // 기본값
            return boolValue ? "True" : "False";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    // MultiBinding의 값들을 객체 배열로 변환하는 컨버터
    public class ArrayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 