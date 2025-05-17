using System;
using System.Windows;
using DLGameViewer.Interfaces;
using DLGameViewer.ViewModels;

namespace DLGameViewer;

/// <summary>
/// MainWindow.xaml에 대한 상호 작용 논리입니다.
/// </summary>
public partial class MainWindow : Window
{
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
    }
}