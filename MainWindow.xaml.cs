using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DLGameViewer.Models; // GameInfo 모델 클래스 사용
using DLGameViewer.Services; // DatabaseService 사용
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
public partial class MainWindow : Window
{
    private readonly DatabaseService _databaseService;
    private readonly FolderScannerService _folderScannerService;
    private readonly WebMetadataService _webMetadataService;
    private readonly ImageService _imageService;
    public ObservableCollection<GameInfo> Games { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _imageService = new ImageService();
        _webMetadataService = new WebMetadataService(_imageService);
        _folderScannerService = new FolderScannerService(_databaseService, _webMetadataService);
        Games = new ObservableCollection<GameInfo>();
        GamesItemsControl.ItemsSource = Games; // XAML의 ItemsControl x:Name과 일치해야 함

        LoadGamesToGrid(); // 시작 시 게임 목록 로드
    }

    private void LoadGamesToGrid()
    {
        Games.Clear(); // 기존 목록 지우기
        try{
            var gamesFromDb = _databaseService.GetAllGames();
            if (gamesFromDb != null){
                foreach (var game in gamesFromDb){
                    Games.Add(game);
                }
            }
        }
        catch (Exception ex){
            MessageBox.Show($"게임 목록을 불러오는 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void btnScanFolder_Click(object sender, RoutedEventArgs e)
    {
        string folderPath = txtScanFolderPath.Text;
        if (string.IsNullOrWhiteSpace(folderPath) || folderPath == "(선택된 폴더 없음)" || !Directory.Exists(folderPath))
        {
            MessageBox.Show("유효한 폴더 경로를 선택하거나 입력해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        txtScanResults.Text = "폴더 스캔 중..."; // XAML의 TextBox x:Name과 일치해야 함
        Button? scanButton = sender as Button; // scanButton을 여기서 한 번만 선언하고 할당
        if (scanButton != null) // 타입 캐스팅 안전성 확보 및 null 체크
        {
            scanButton.IsEnabled = false;
        }
        try
        {
            List<GameInfo> scannedGames = await _folderScannerService.ScanDirectoryAsync(folderPath);
            StringBuilder sb = new StringBuilder();
            if (scannedGames.Any())
            {
                sb.AppendLine($"{scannedGames.Count}개의 게임 정보를 폴더에서 찾았습니다:");
                foreach (var game in scannedGames)
                {
                    sb.AppendLine($"- 식별자: {game.Identifier}, 폴더명: {System.IO.Path.GetFileName(game.FolderPath)}, 실행파일 수: {game.ExecutableFiles.Count}");
                    // 스캔된 게임을 바로 DB에 추가하고 UI 갱신 (선택적)
                    GameInfo? fetchedGameInfo = await _webMetadataService.FetchMetadataAsync(game.Identifier);
                    if (fetchedGameInfo != null){
                        _databaseService.AddGame(fetchedGameInfo);
                    }
                    else {
                        Console.WriteLine($"웹에서 {game.Identifier}에 대한 메타데이터를 가져오지 못했습니다. 로컬 정보만 사용합니다.");
                        _databaseService.AddGame(game);
                    }
                }
                LoadGamesToGrid(); // 스캔 후 목록 자동 갱신이 필요하면 주석 해제 (DB에 추가 후)
            }
            else
            {
                sb.AppendLine("지정된 폴더에서 인식 가능한 게임 정보를 찾지 못했습니다.");
            }
            txtScanResults.Text = sb.ToString();
            MessageBox.Show(sb.ToString(), "폴더 스캔 완료", MessageBoxButton.OK, MessageBoxImage.Information);

        }
        catch (Exception ex)
        {
            txtScanResults.Text = $"스캔 중 오류 발생: {ex.Message}";
            MessageBox.Show($"폴더 스캔 중 오류 발생: {ex.Message}", "스캔 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (scanButton != null) // 기존 scanButton 변수 사용
            {
                scanButton.IsEnabled = true;
            }
        }
    }

    private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog // Microsoft.Win32.OpenFolderDialog 사용
        {
            Title = "스캔할 폴더를 선택하세요"
        };

        // ShowDialog()는 nullable bool (bool?)을 반환합니다.
        if (dialog.ShowDialog() == true)
        {
            txtScanFolderPath.Text = dialog.FolderName; // 선택된 폴더 경로
        }
    }

    private void btnRefreshGameList_Click(object sender, RoutedEventArgs e)
    {
        LoadGamesToGrid(); // 게임 목록 새로고침
    }
}