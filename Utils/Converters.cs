using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DLGameViewer.Utils {
    // 로컬 경로를 이미지 소스로 변환하는 컨버터
    public class PathToImageSourceConverter : IValueConverter {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string localPath = value as string ?? string.Empty;
            string parameterString = parameter as string ?? string.Empty;

            if (string.IsNullOrWhiteSpace(localPath)) {
                return null;
            }

            int targetWidth;
            if (parameterString == "ListImageContent") {
                targetWidth = 200;
            } else if (parameterString == "MainImageContent") {
                targetWidth = 560;
            } else if (parameterString == "PreviewImageContent") {
                targetWidth = 100;
            } else {
                targetWidth = 560;
            }

            try {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(localPath, UriKind.Absolute);
                bitmap.DecodePixelWidth = targetWidth;
                bitmap.CacheOption = BitmapCacheOption.OnLoad; 
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // 캐시 문제 방지
                bitmap.EndInit();
                bitmap.Freeze(); // UI 스레드 외 접근 및 성능 향상
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
} 