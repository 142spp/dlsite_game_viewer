using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DLGameViewer.Converters
{
    public class LocalPathToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string localPath && !string.IsNullOrWhiteSpace(localPath))
            {
                // 경로가 절대 경로인지 확인하고, 그렇지 않다면 애플리케이션 기본 디렉토리를 기준으로 변환 시도
                // (ImageService에서 이미 절대 경로를 저장하고 있다면 이 부분은 덜 중요할 수 있음)
                string absolutePath = localPath;
                if (!Path.IsPathRooted(localPath))
                {
                    // 주의: 이 방식은 LocalImagePath가 항상 실행 파일 기준 상대 경로일 때만 유효합니다.
                    // 서비스에서 절대 경로를 저장하는 것이 더 견고합니다.
                    // 여기서는 일단 localPath가 절대 경로라고 가정하거나, 매우 단순한 상대 경로 변환만 시도합니다.
                    // absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localPath); 
                    // 더 나은 방법은 LocalImagePath가 항상 절대 경로임을 보장하는 것입니다.
                }

                if (File.Exists(absolutePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
                        bitmap.DecodePixelWidth = 180; // XAML에 있던 값 유지 또는 파라미터화 가능
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // 파일 락 방지 및 즉시 로드
                        bitmap.EndInit();
                        bitmap.Freeze(); // UI 스레드 외 접근 및 성능 향상
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        // 이미지 로딩 실패 (잘못된 형식, 손상된 파일 등)
                        Console.WriteLine($"Error loading image from {absolutePath}: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    // 파일이 존재하지 않음
                    // Console.WriteLine($"Image file not found at {absolutePath}"); // 필요시 로그 활성화
                    return null;
                }
            }
            // 경로가 null이거나 비어있거나 문자열이 아님
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 