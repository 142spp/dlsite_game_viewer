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
using System.ComponentModel; // INotifyPropertyChanged
using System.Xaml; // XAML 관련

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

    private void UpdateGamesCollection(GameInfo gameInfo, long gameId) {
        // 기존 목록을 유지하면서 새 게임 추가
        gameInfo.Id = gameId; // ID 설정 (데이터베이스 ID)
        // 중복 방지 (이미 있는 ID는 추가하지 않음)
        if (!Games.Any(g => g.Id == gameId)) {
            Games.Add(gameInfo);
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
        // 스캔 시작 전 결과 영역 초기화
        txtScanResults.Clear();
        List<GameInfo> scannedGames = new List<GameInfo>();
        Exception? caughtException = null;
        try {
            // 콜백 정의: 실시간 진행 상황 업데이트를 위한 함수
            ScanProcessHelper.ProgressUpdateCallback progressCallback = (message, gameInfo, gameId) => {
                // UI 스레드에서 실행 (Dispatcher 사용)
                Dispatcher.Invoke(() => {
                    txtScanResults.AppendText(message + Environment.NewLine); // 결과 텍스트 영역에 메시지 추가
                    txtScanResults.ScrollToEnd(); // 스크롤을 항상 아래로 유지
                    // 새 게임이 추가된 경우 UI 업데이트
                    if (gameInfo != null && gameId > 0) {
                        UpdateGamesCollection(gameInfo, gameId);
                    }
                });
            };
            // ScanProcessHelper의 static 메서드 호출 (콜백 전달)
            await ScanProcessHelper.ProcessFolderScanAsync(
                folderPath, 
                scannedGames, 
                _folderScannerService, 
                _webMetadataService, 
                _databaseService,
                progressCallback
            );
            // 스캔 완료 후 게임 목록 갱신 (누락된 항목이나 데이터베이스 상태와 동기화하기 위해)
            if (scannedGames.Any()) {
                LoadGamesToGrid();
            }
        } catch (Exception ex) {
            caughtException = ex;
            string errorMessage = $"스캔 처리 중 심각한 오류 발생: {ex.Message}";
            txtScanResults.Text = errorMessage;
            MessageBox.Show($"폴더 스캔 처리 중 오류 발생: {ex.Message}", "스캔 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        } finally {
            // 스캔이 성공적으로 완료되면 메시지 표시
            if (scannedGames.Any() && caughtException == null) {
                MessageBox.Show($"폴더 스캔이 완료되었습니다. {scannedGames.Count}개의 게임을 발견했습니다.", 
                               "폴더 스캔 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            } else if (!scannedGames.Any() && caughtException == null) {
                MessageBox.Show("지정된 폴더에서 인식 가능한 게임 정보를 찾지 못했습니다.", 
                               "폴더 스캔 결과", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void MenuItemRunGame_Click(object sender, RoutedEventArgs e) {
        MenuItem menuItem = (MenuItem)sender;
        ContextMenu contextMenu = (ContextMenu)menuItem.Parent;
        Border border = (Border)contextMenu.PlacementTarget;
        GameInfo game = (GameInfo)border.DataContext;

        if (game == null) {
            MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 게임 실행 로직을 Helper 클래스로 위임
        GameExecutionHelper.RunGame(game, this);
    }

    private void MenuItemGameInfo_Click(object sender, RoutedEventArgs e) {
        MenuItem menuItem = (MenuItem)sender;
        ContextMenu contextMenu = (ContextMenu)menuItem.Parent;
        Border border = (Border)contextMenu.PlacementTarget;
        GameInfo game = (GameInfo)border.DataContext;

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
                int updatedRows = _databaseService.UpdateGame(infoDialog.Game);
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

    private void MenuItemOpenFolder_Click(object sender, RoutedEventArgs e) {
        MenuItem menuItem = (MenuItem)sender;
        ContextMenu contextMenu = (ContextMenu)menuItem.Parent;
        Border border = (Border)contextMenu.PlacementTarget;
        GameInfo game = (GameInfo)border.DataContext;

        if (game == null || string.IsNullOrEmpty(game.FolderPath)) {
            MessageBox.Show("폴더 경로를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try {
            // 폴더 열기 - 특수 문자 처리를 위해 ProcessStartInfo 사용
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{game.FolderPath}\"", // 따옴표로 경로 감싸기
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        } catch (Exception ex) {
            MessageBox.Show($"폴더를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuItemDeleteFromDb_Click(object sender, RoutedEventArgs e) {
        MenuItem menuItem = (MenuItem)sender;
        ContextMenu contextMenu = (ContextMenu)menuItem.Parent;
        Border border = (Border)contextMenu.PlacementTarget;
        GameInfo game = (GameInfo)border.DataContext;

        if (game == null) {
            MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 삭제 확인 대화상자
        var result = MessageBox.Show(
            $"정말로 '{game.Title}' 게임을 데이터베이스에서 삭제하시겠습니까?\n\n" +
            "이 작업은 취소할 수 없습니다.",
            "삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes) {
            try {
                int deletedRows = _databaseService.DeleteGame(game.Id);
                if (deletedRows > 0) {
                    LoadGamesToGrid(); // UI 새로고침
                    MessageBox.Show("게임이 성공적으로 삭제되었습니다.", "삭제 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show("게임 삭제에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } catch (Exception ex) {
                MessageBox.Show($"게임 삭제 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void btnClearDatabase_Click(object sender, RoutedEventArgs e) {
        // 초기화 확인 대화상자
        var result = MessageBox.Show(
            "정말로 데이터베이스를 초기화하시겠습니까?\n\n" +
            "이 작업은 모든 게임 정보를 삭제하며 취소할 수 없습니다.",
            "데이터베이스 초기화 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes) {
            try {
                // 한번 더 확인
                var secondResult = MessageBox.Show(
                    "정말 확실합니까? 모든 데이터가 삭제됩니다.",
                    "최종 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Exclamation);

                if (secondResult == MessageBoxResult.Yes) {
                    // 데이터베이스 초기화 실행
                    int deletedRows = _databaseService.ClearAllGames();
                    
                    // UI 갱신
                    LoadGamesToGrid();
                    
                    // 결과 메시지 표시
                    MessageBox.Show(
                        $"데이터베이스가 초기화되었습니다. {deletedRows}개의 항목이 삭제되었습니다.",
                        "초기화 완료",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"데이터베이스 초기화 중 오류가 발생했습니다: {ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}