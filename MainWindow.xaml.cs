using System;
using System.Windows;
using System.Windows.Controls;
using DLGameViewer.Interfaces;
using DLGameViewer.ViewModels;
using System.Linq;

namespace DLGameViewer;

/// <summary>
/// MainWindow.xaml에 대한 상호 작용 논리입니다.
/// </summary>
public partial class MainWindow : Window
{
    private bool _isComposing;

    public MainWindow(
        IDatabaseService databaseService,
        IFolderScannerService folderScannerService,
        IWebMetadataService webMetadataService,
        IImageService imageService)
    {
        InitializeComponent();
        
        // ViewModel 생성 및 DataContext 설정
        DataContext = new MainViewModel(
            databaseService,
            folderScannerService,
            webMetadataService,
            imageService);

        // Loaded 이벤트 핸들러를 등록합니다.
        this.Loaded += MainWindow_Loaded;
    }

    // Loaded 이벤트 핸들러를 추가합니다.
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            // 주요 메뉴 커맨드 워밍업
            _ = vm.ScanFolderCommand.CanExecute(null);
            _ = vm.RefreshGameListCommand.CanExecute(null);
            _ = vm.ClearDatabaseCommand.CanExecute(null);
            _ = vm.ToggleThemeCommand.CanExecute(null);
            _ = vm.AboutCommand.CanExecute(null);
            _ = vm.ExitApplicationCommand.CanExecute(null);

            // 검색/정렬 관련 커맨드 워밍업
            _ = vm.ClearSearchCommand.CanExecute(null);
            if (vm.SortFields != null && vm.SortFields.Any())
            {
                _ = vm.SortCommand.CanExecute(vm.SortFields.FirstOrDefault());
            }
            else
            {
                _ = vm.SortCommand.CanExecute(null); // SortFields가 비어있을 경우
            }
            
            // 워밍업 실행 확인용 디버그 메시지
            System.Diagnostics.Debug.WriteLine("[MainWindow] ViewModel commands warmed up on Loaded event.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] MainViewModel not found in DataContext during Loaded event.");
        }
    }
}