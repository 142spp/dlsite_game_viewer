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

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        SetInitialWindowSize();
    }

    private void SetInitialWindowSize()
    {
        // 주 모니터의 작업 영역 크기를 가져옵니다. (작업 표시줄 제외)
        double screenWidth = SystemParameters.WorkArea.Width;
        double screenHeight = SystemParameters.WorkArea.Height;

        // 창 크기를 화면의 일정 비율로 설정합니다.
        Width = screenWidth * 0.6;
        Height = screenHeight * 0.9;

        // 창의 최소 크기를 설정하여 너무 작아지는 것을 방지합니다.
        MinWidth = 800;
        MinHeight = 600;

        // 창을 화면 중앙에 위치시킵니다.
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}