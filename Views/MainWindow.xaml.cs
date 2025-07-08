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
    }
}