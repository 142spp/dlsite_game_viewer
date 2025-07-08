using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DLGameViewer.Helpers;
using DLGameViewer.Interfaces;
using DLGameViewer.Services;
using DLGameViewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DLGameViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // 서비스 프로바이더
    public static IServiceProvider? ServiceProvider { get; private set; }

    // DWM API를 사용해 윈도우 제목표시줄 색상 제어
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 의존성 주입 설정
        ConfigureServices();
        
        // 폰트 직접 등록 - 로딩 실패 시 백업 방법
        try {
            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Fonts", "NotoSansKR-Regular.ttf");
            if (File.Exists(fontPath)) {
                // 출력 폴더에 폰트 파일이 존재하는 경우
                Console.WriteLine($"폰트 파일 존재: {fontPath}");
                
                // 프로그램 실행 위치에 있는 폰트를 직접 등록
                var customFont = new FontFamily("pack://application:,,,/Resources/Fonts/NotoSansKR-Regular.ttf#Noto Sans KR");
                Resources["NotoSansKR"] = customFont;
                Console.WriteLine("외부 폰트 직접 등록 완료");
            }
            else {
                Console.WriteLine($"폰트 파일 없음: {fontPath}");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"폰트 등록 오류: {ex.Message}");
        }
        
        // 테마 변경 이벤트 구독
        ThemeManager.ThemeChanged += OnThemeChanged;
        
        // 시작 시 현재 테마에 맞게 제목표시줄 설정
        SetTitleBarTheme(ThemeManager.CurrentTheme);

        // 메인 윈도우 생성 및 표시
        var mainWindow = ServiceProvider?.GetRequiredService<MainWindow>();
        mainWindow?.Show();
    }

    /// <summary>
    /// 의존성 주입 서비스를 구성합니다.
    /// </summary>
    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // 서비스 등록
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IWebMetadataService, WebMetadataService>();
        services.AddSingleton<IFolderScannerService, FolderScannerService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels 등록
        services.AddTransient<MainViewModel>();

        // Views 등록
        services.AddTransient<MainWindow>();

        // 서비스 프로바이더 생성
        ServiceProvider = services.BuildServiceProvider();
    }
    
    private void OnThemeChanged(object? sender, ThemeType theme)
    {
        // 테마가 변경될 때 제목표시줄 스타일 변경
        SetTitleBarTheme(theme);
    }
    
    public static void SetTitleBarTheme(ThemeType theme)
    {
        // 윈도우 버전 확인 (Windows 10 1809 이상에서만 동작)
        if (IsWindows10OrGreater())
        {
            foreach (Window window in Current.Windows)
            {
                var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                if (handle != IntPtr.Zero)
                {
                    SetWindowTitleBarDarkMode(handle, theme == ThemeType.Dark);
                }
            }
        }
    }
    
    private static bool IsWindows10OrGreater()
    {
        // Windows 10 버전 확인
        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (reg != null)
        {
            string productName = (string)reg.GetValue("ProductName", "");
            return productName.StartsWith("Windows 10") || productName.StartsWith("Windows 11");
        }
        return false;
    }
    
    public static void SetWindowTitleBarDarkMode(IntPtr handle, bool darkMode)
    {
        int attribute = IsWindows10BuildOrGreater(20000) ? DWMWA_USE_IMMERSIVE_DARK_MODE : DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
        int value = darkMode ? 1 : 0;
        DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
    }
    
    private static bool IsWindows10BuildOrGreater(int buildNumber)
    {
        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (reg != null)
        {
            try
            {
                var currentBuildStr = (string)reg.GetValue("CurrentBuild", "0");
                if (int.TryParse(currentBuildStr, out int currentBuild))
                {
                    return currentBuild >= buildNumber;
                }
            }
            catch { }
        }
        return false;
    }
}

