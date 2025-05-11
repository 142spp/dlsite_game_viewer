using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DLGameViewer.Converters {
    public class LocalPathToImageSourceConverter : IValueConverter {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Console.WriteLine($"{parameter}");
            if (value is string localPath && !string.IsNullOrWhiteSpace(localPath)) {
                string imagePath = localPath;

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)) {
                return null;
            }

            string parameterString = parameter as string;
            if (string.IsNullOrWhiteSpace(parameterString)) {
                return null;
            }

            int targetWidth = 0;
            if (parameterString == "ListImageContent") {
                targetWidth = 200;
            } else if (parameterString == "MainImageContent") {
                targetWidth = 560;
            } else if (parameterString == "PreviewImageContent") {
                targetWidth = 100;
            }

            try {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.DecodePixelWidth = targetWidth;
                bitmap.CacheOption = BitmapCacheOption.OnLoad; 
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // 캐시 문제 방지
                bitmap.EndInit();
                bitmap.Freeze(); // UI 스레드 외 접근 및 성능 향상
                return bitmap;
            } catch (Exception ex) {
                    // 이미지 로딩 실패 (잘못된 형식, 손상된 파일 등)
                    Console.WriteLine($"Error loading image from {imagePath}: {ex.Message}");
                    return null;
                }
            }
            return null;
        }
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}