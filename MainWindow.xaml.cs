using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DLGameViewer.Models; // GameInfo 모델 클래스 사용
using DLGameViewer.Services; // DatabaseService 사용
using DLGameViewer.Helpers; // 헬퍼 클래스 사용
using DLGameViewer.Dialogs; // Dialogs 네임스페이스 추가
using System.Collections.Generic;
using System.Threading.Tasks; // 비동기 작업
using System.IO; // Directory.Exists
using System.Linq; // For .Any()
using System.Collections.ObjectModel; // ObservableCollection
using Microsoft.Win32; // OpenFolderDialog 사용을 위해 추가

namespace DLGameViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private readonly DatabaseService _databaseService;
    private readonly FolderScannerService _folderScannerService;
    private readonly WebMetadataService _webMetadataService;
    private readonly ImageService _imageService;
    public ObservableCollection<GameInfo> Games { get; set; }
    private CancellationTokenSource _scanAnimationCts = new CancellationTokenSource();


    public MainWindow() {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _imageService = new ImageService();
        _webMetadataService = new WebMetadataService(_imageService);
        _folderScannerService = new FolderScannerService(_databaseService, _webMetadataService);
        Games = new ObservableCollection<GameInfo>();
        GamesItemsControl.ItemsSource = Games; // XAML의 ItemsControl x:Name과 일치해야 함

        LoadGamesToGrid(); // 시작 시 게임 목록 로드
    }

    private void LoadGamesToGrid() {
        Games.Clear(); // 기존 목록 지우기
        try {
            var gamesFromDb = _databaseService.GetAllGames();
            if (gamesFromDb != null) {
                foreach (var game in gamesFromDb) {
                    Games.Add(game);
                }
            }
        } catch (Exception ex) {
            MessageBox.Show($"게임 목록을 불러오는 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AnimateScanMessage(CancellationToken token) {
        string baseMessage = "폴더 스캔 중";
        int dotCount = 0;
        while (!token.IsCancellationRequested) {
            dotCount = (dotCount + 1) % 4;
            string dots = new string('.', dotCount);
            txtScanResults.Text = baseMessage + dots;
            try {
                await Task.Delay(500, token); // 0.5초 간격으로 업데이트
            } catch (TaskCanceledException) {
                // 작업 취소 시 루프 종료
                break;
            }
        }
    }

    private async void btnScanFolder_Click(object sender, RoutedEventArgs e) {
        string folderPath = txtScanFolderPath.Text;
        if (string.IsNullOrWhiteSpace(folderPath) || folderPath == "(선택된 폴더 없음)" || !Directory.Exists(folderPath)) {
            MessageBox.Show("유효한 폴더 경로를 선택하거나 입력해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Button? scanButton = sender as Button;
        if (scanButton != null) {
            scanButton.IsEnabled = false;
        }

        _scanAnimationCts = new CancellationTokenSource();
        Task animationTask = AnimateScanMessage(_scanAnimationCts.Token);

        StringBuilder resultsSb = new StringBuilder();
        List<GameInfo> scannedGames = new List<GameInfo>();
        Exception? caughtException = null;

        try {
            // ScanProcessHelper의 static 메서드 호출
            resultsSb = await ScanProcessHelper.ProcessFolderScanAsync(folderPath, scannedGames, _folderScannerService, _webMetadataService, _databaseService);
            
            if (scannedGames.Any()) {
                LoadGamesToGrid();
            }
        } catch (Exception ex) {
            caughtException = ex;
            resultsSb.Clear();
            resultsSb.AppendLine($"스캔 처리 중 심각한 오류 발생: {ex.Message}");
            MessageBox.Show($"폴더 스캔 처리 중 오류 발생: {ex.Message}", "스캔 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        } finally {
            _scanAnimationCts.Cancel();
            try {
                await animationTask;
            } catch (OperationCanceledException) { /* TaskCanceledException is a subtype of OperationCanceledException, so this catches both */ }

            txtScanResults.Text = resultsSb.ToString();
            if (scannedGames.Any() && caughtException == null) {
                MessageBox.Show(resultsSb.ToString(), "폴더 스캔 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            } else if (!scannedGames.Any() && caughtException == null) {
                 MessageBox.Show(resultsSb.ToString(), "폴더 스캔 결과", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (scanButton != null) {
                scanButton.IsEnabled = true;
            }
        }
    }

    private void btnBrowseFolder_Click(object sender, RoutedEventArgs e) {
        var dialog = new OpenFolderDialog // Microsoft.Win32.OpenFolderDialog 사용
        {
            Title = "스캔할 폴더를 선택하세요"
        };

        // ShowDialog()는 nullable bool (bool?)을 반환합니다.
        if (dialog.ShowDialog() == true) {
            txtScanFolderPath.Text = dialog.FolderName; // 선택된 폴더 경로
        }
    }

    private void btnRefreshGameList_Click(object sender, RoutedEventArgs e) {
        LoadGamesToGrid(); // 게임 목록 새로고침
    }

    private void btnRunGame_Click(object sender, RoutedEventArgs e) {
        Button button = (Button)sender;
        GameInfo game = (GameInfo)button.DataContext;

        if (game == null) {
            MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 게임 실행 로직을 Helper 클래스로 위임
        GameExecutionHelper.RunGame(game, this);
    }

    private void btnGameInfo_Click(object sender, RoutedEventArgs e) {
        Button? button = sender as Button;
        if (button == null) return;

        GameInfo? game = button.DataContext as GameInfo;
        if (game == null) {
            MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // GameInfoDialog를 현재 MainWindow를 Owner로 하여 생성
        GameInfoDialog infoDialog = new GameInfoDialog(game) {
            Owner = this
        };

        if (infoDialog.ShowDialog() == true) {
            // Dialog에서 Game 객체가 수정되었으므로, 해당 객체로 DB 업데이트
            try {
                int updatedRows = _databaseService.UpdateGame(infoDialog.Game); // Dialog의 Game 프로퍼티 사용
                if (updatedRows > 0) {
                    LoadGamesToGrid(); // UI 새로고침
                    MessageBox.Show("게임 정보가 성공적으로 업데이트되었습니다.", "정보 업데이트", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show("게임 정보 업데이트에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } catch (Exception ex) {
                MessageBox.Show($"게임 정보 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}