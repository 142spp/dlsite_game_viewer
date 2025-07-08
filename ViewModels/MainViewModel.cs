using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DLGameViewer.Dialogs;
using DLGameViewer.Helpers;
using DLGameViewer.Interfaces;
using DLGameViewer.Models;
using Microsoft.Win32;

namespace DLGameViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IFolderScannerService _folderScannerService;
        private readonly IWebMetadataService _webMetadataService;
        private readonly IImageService _imageService;
        private readonly ISettingsService _settingsService;
        private bool _isLoading;
        private bool _isScanning;
        private ThemeType _currentTheme;
        private GameInfo? _selectedGame;
        private string _searchText = string.Empty;
        private string _statusMessage = string.Empty;
        private string _sortField = "제목";
        private bool _isAscending = false;
        private string _searchField = "전체";
        private CancellationTokenSource? _searchCancellationSource;
        private CancellationTokenSource? _scanCancellationTokenSource;
        private const int DEBOUNCE_DELAY_MS = 500;
        private ObservableCollection<GameInfo> _games;
        private readonly SemaphoreSlim _loadGamesLock = new SemaphoreSlim(1, 1);
        private string _scanResultText = string.Empty;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalGameCount;
        private List<int> _pageSizes = new List<int> { 10, 20, 30, 50, 100, 200 };

        public string ScanResultText { get => _scanResultText; set => SetProperty(ref _scanResultText, value); }
        public ObservableCollection<GameInfo> Games { get => _games; set => SetProperty(ref _games, value); }
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (RefreshGameListCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (DeleteGameCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (ClearDatabaseCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (SortCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (RefreshGameDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (ChangePageSizeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    });
                }
            }
        }
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        (ScanFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (StopScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (RefreshGameListCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        (ClearDatabaseCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    });
                }
            }
        }
        public ThemeType CurrentTheme { get => _currentTheme; set => SetProperty(ref _currentTheme, value); }
        public GameInfo? SelectedGame { get => _selectedGame; set => SetProperty(ref _selectedGame, value); }
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    (ClearSearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    _searchCancellationSource?.Cancel();
                    _searchCancellationSource = new CancellationTokenSource();
                    var token = _searchCancellationSource.Token;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(DEBOUNCE_DELAY_MS, token);
                            if (!token.IsCancellationRequested)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    CurrentPage = 1;
                                    await LoadGamesToGridAsync();
                                });
                            }
                        }
                        catch (TaskCanceledException) { }
                        catch (Exception ex) { Debug.WriteLine($"SearchText 지연 로딩 중 오류: {ex.Message}"); }
                    });
                }
            }
        }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string SortField
        {
            get => _sortField;
            set
            {
                if (SetProperty(ref _sortField, value))
                {
                    CurrentPage = 1;
                    _ = Task.Run(async () =>
                    {
                        try { await Application.Current.Dispatcher.InvokeAsync(async () => { await LoadGamesToGridAsync(); }); }
                        catch (Exception ex) { Debug.WriteLine($"SortField 변경 시 로딩 오류: {ex.Message}"); }
                    });
                }
            }
        }
        public bool IsAscending
        {
            get => _isAscending;
            set
            {
                if (SetProperty(ref _isAscending, value))
                {
                    CurrentPage = 1;
                    _ = Task.Run(async () =>
                    {
                        try { await Application.Current.Dispatcher.InvokeAsync(async () => { await LoadGamesToGridAsync(); }); }
                        catch (Exception ex) { Debug.WriteLine($"IsAscending 변경 시 로딩 오류: {ex.Message}"); }
                    });
                }
            }
        }
        public string SearchField
        {
            get => _searchField;
            set
            {
                if (SetProperty(ref _searchField, value))
                {
                    CurrentPage = 1;
                    if (!string.IsNullOrEmpty(SearchText) || !string.IsNullOrEmpty(value))
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await Application.Current.Dispatcher.InvokeAsync(async () => { await LoadGamesToGridAsync(); }); }
                            catch (Exception ex) { Debug.WriteLine($"SearchField 변경 시 로딩 오류: {ex.Message}"); }
                        });
                    }
                }
            }
        }
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(CanGoPreviousPage));
                    OnPropertyChanged(nameof(CanGoNextPage));
                    (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    OnPropertyChanged(nameof(TotalPages));
                    _settingsService.Settings.PageSize = value;
                    _settingsService.SaveSettings();
                    _ = Task.Run(async () =>
                    {
                        try { await Application.Current.Dispatcher.InvokeAsync(async () => { await LoadGamesToGridAsync(); }); }
                        catch (Exception ex) { Debug.WriteLine($"PageSize 변경 시 로딩 오류: {ex.Message}"); }
                    });
                }
            }
        }
        public List<int> PageSizes { get => _pageSizes; set => SetProperty(ref _pageSizes, value); }
        public int TotalGameCount
        {
            get => _totalGameCount;
            set
            {
                if (SetProperty(ref _totalGameCount, value))
                {
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(CanGoPreviousPage));
                    OnPropertyChanged(nameof(CanGoNextPage));
                    (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        public int TotalPages => (TotalGameCount + PageSize - 1) / PageSize;
        public bool CanGoPreviousPage => CurrentPage > 1;
        public bool CanGoNextPage => CurrentPage < TotalPages;

        public ICommand ScanFolderCommand { get; private set; }
        public ICommand RefreshGameListCommand { get; private set; }
        public ICommand GameInfoCommand { get; private set; }
        public ICommand RunGameCommand { get; private set; }
        public ICommand OpenFolderCommand { get; private set; }
        public ICommand DeleteGameCommand { get; private set; }
        public ICommand ClearDatabaseCommand { get; private set; }
        public ICommand ToggleThemeCommand { get; private set; }
        public ICommand ExitApplicationCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand SortCommand { get; private set; }
        public ICommand StopScanCommand { get; private set; }
        public ICommand RefreshGameDataCommand { get; private set; }
        public ICommand OpenDlSiteCommand { get; private set; }
        public ICommand OpenSaveFolderCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        public ICommand PreviousPageCommand { get; private set; }
        public ICommand GoToPageCommand { get; private set; }
        public ICommand ChangePageSizeCommand { get; private set; }

        public List<string> SearchFields { get; }
        public List<string> SortFields { get; }

        public MainViewModel(IDatabaseService databaseService, IFolderScannerService folderScannerService, IWebMetadataService webMetadataService, IImageService imageService, ISettingsService settingsService)
        {
            _databaseService = databaseService;
            _databaseService.InitializeDatabaseAsync();
            _folderScannerService = folderScannerService;
            _webMetadataService = webMetadataService;
            _imageService = imageService;
            _settingsService = settingsService;

            _games = new ObservableCollection<GameInfo>();

            PageSize = _settingsService.Settings.PageSize;
            ThemeManager.ApplyTheme(_settingsService.Settings.CurrentTheme);
            CurrentTheme = _settingsService.Settings.CurrentTheme;

            SearchText = string.Empty;
            StatusMessage = "준비";

            SearchFields = new List<string> { "전체", "제목", "식별자", "제작자", "장르", "게임 타입" };
            SortFields = new List<string> { "제목", "식별자", "제작자", "판매량", "평점", "평점수", "출시일", "추가일", "파일 크기", "최근 실행", "플레이 시간" };

            ScanFolderCommand = new RelayCommand(async _ => await ScanFolderAsync(), _ => !IsScanning);
            StopScanCommand = new RelayCommand(async _ => await StopScanAsync(), _ => IsScanning);
            RefreshGameListCommand = new RelayCommand(async _ => { CurrentPage = 1; await LoadGamesToGridAsync(); }, _ => !IsLoading && !IsScanning);
            GameInfoCommand = new RelayCommand(async param => await ShowGameInfo(param as GameInfo), param => param is GameInfo);
            RunGameCommand = new RelayCommand(param => RunGame(param as GameInfo), param => param is GameInfo);
            OpenFolderCommand = new RelayCommand(param => OpenFolder(param as GameInfo), param => param is GameInfo);
            DeleteGameCommand = new RelayCommand(async param => await DeleteGameUiAsync(param as GameInfo), param => param is GameInfo && !IsLoading);
            ClearDatabaseCommand = new RelayCommand(async _ => await ClearDatabaseUiAsync(), _ => !IsLoading && !IsScanning);
            ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
            ExitApplicationCommand = new RelayCommand(_ => ExitApplication());
            AboutCommand = new RelayCommand(_ => ShowAboutInfo());
            ClearSearchCommand = new RelayCommand(async _ => await ClearSearchAsync(), _ => !string.IsNullOrEmpty(SearchText) && !IsLoading);
            SortCommand = new RelayCommand(async param => await SortAsync(param as string), param => param is string && !IsLoading);
            RefreshGameDataCommand = new RelayCommand(async param => await RefreshGameDataAsync(param as GameInfo), param => param is GameInfo && !IsLoading);
            OpenDlSiteCommand = new RelayCommand(param => OpenDlSite(param as GameInfo), param => param is GameInfo);
            OpenSaveFolderCommand = new RelayCommand(param => OpenSaveFolder(param as GameInfo), param => param is GameInfo && !string.IsNullOrEmpty(((GameInfo)param).SaveFolderPath));
            NextPageCommand = new RelayCommand(async _ => await ChangePageAsync(CurrentPage + 1), _ => CanGoNextPage && !IsLoading);
            PreviousPageCommand = new RelayCommand(async _ => await ChangePageAsync(CurrentPage - 1), _ => CanGoPreviousPage && !IsLoading);
            ChangePageSizeCommand = new RelayCommand(async selectedPageSize =>
            {
                if (selectedPageSize is int newSize) { PageSize = newSize; }
            }, _ => !IsLoading);

            _ = Task.Run(async () =>
            {
                try { await Application.Current.Dispatcher.InvokeAsync(async () => { await LoadGamesToGridAsync(); }); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"초기 로딩 오류: {ex.Message}");
                    Application.Current.Dispatcher.Invoke(() => { StatusMessage = "초기 데이터 로드 중 오류 발생"; });
                }
            });
        }

        private async Task StopScanAsync()
        {
            if (_scanCancellationTokenSource != null)
            {
                StatusMessage = "스캔 중지를 요청하는 중...";
                _scanCancellationTokenSource.Cancel();
            }
            await Task.CompletedTask;
        }

        private async Task ChangePageAsync(int newPageNumber)
        {
            if (newPageNumber > 0 && newPageNumber <= TotalPages && newPageNumber != CurrentPage)
            {
                CurrentPage = newPageNumber;
                await LoadGamesToGridAsync();
            }
        }

        private string GetDbSortField(string uiSortField)
        {
            return uiSortField switch
            {
                "제목" => "Title", "식별자" => "Identifier", "제작자" => "Creator", "판매량" => "SalesCount",
                "평점" => "Rating", "평점수" => "RatingCount", "추가일" => "DateAdded", "최근 실행" => "LastPlayed",
                "출시일" => "ReleaseDate", "파일 크기" => "FileSize", "플레이 시간" => "PlayTime", _ => "DateAdded",
            };
        }

        private string GetDbSearchField(string uiSearchField)
        {
            return uiSearchField switch
            {
                "전체" => "All", "제목" => "Title", "식별자" => "Identifier", "제작자" => "Creator",
                "장르" => "Genres", "게임 타입" => "GameType", _ => "All",
            };
        }

        private async Task LoadGamesToGridAsync()
        {
            if (IsLoading && _loadGamesLock.CurrentCount == 0) return;
            await _loadGamesLock.WaitAsync();
            try
            {
                IsLoading = true;
                StatusMessage = $"페이지 {CurrentPage} 로드 중...";
                string dbSortField = GetDbSortField(SortField);
                string dbSearchField = GetDbSearchField(SearchField);
                List<GameInfo> gamesFromDb = await _databaseService.GetGamesAsync(CurrentPage, PageSize, dbSortField, IsAscending, SearchText, dbSearchField);
                TotalGameCount = await _databaseService.GetTotalGameCountAsync(SearchText, dbSearchField);
                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Games.Clear();
                        foreach (var game in gamesFromDb) { Games.Add(game); }
                    });
                }
                else
                {
                    Games.Clear();
                    foreach (var game in gamesFromDb) { Games.Add(game); }
                }
                StatusMessage = TotalGameCount > 0 ? $"페이지 {CurrentPage}/{TotalPages}" : "표시할 게임 없음";
            }
            catch (Exception ex)
            {
                StatusMessage = $"게임 목록 로드 중 오류: {ex.Message}";
                Debug.WriteLine($"LoadGamesToGridAsync Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                _loadGamesLock.Release();
            }
        }

        private async Task ClearSearchAsync() { SearchText = string.Empty; }

        private async Task SortAsync(string? field)
        {
            if (string.IsNullOrWhiteSpace(field) || IsLoading) return;
            if (SortField == field) { }
            else
            {
                SortField = field;
                IsAscending = true;
            }
            CurrentPage = 1;
            await LoadGamesToGridAsync();
        }

        private void UpdateGamesCollection(GameInfo gameInfo, long gameId)
        {
            gameInfo.Id = gameId;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!Games.Any(g => g.Id == gameId))
                {
                    Games.Add(gameInfo);
                    StatusMessage = $"스캔 중: {Games.Count}개 게임 발견";
                }
            });
        }

        private async Task ScanFolderAsync()
        {
            IsScanning = true;
            _scanCancellationTokenSource = new CancellationTokenSource();
            var token = _scanCancellationTokenSource.Token;
            StatusMessage = "폴더 스캔 중...";
            var scanResultDialog = new ScanResultDialog { Owner = Application.Current.MainWindow, DataContext = this };
            ScanResultText = string.Empty;
            List<GameInfo> scannedGames = new List<GameInfo>();
            Exception? caughtException = null;
            string folderPath = string.Empty;
            try
            {
                var dialog = new OpenFolderDialog { Title = "스캔할 폴더를 선택하세요" };
                if (dialog.ShowDialog() != true)
                {
                    StatusMessage = "폴더 스캔 취소됨";
                    return;
                }
                scanResultDialog.Show();
                folderPath = dialog.FolderName;
                ScanResultText += $"선택한 폴더: {folderPath}\n";
                ScanResultText += "스캔을 시작합니다...\n";
                ScanProcessHelper.ProgressUpdateCallback progressCallback = (message, gameInfo, gameId) =>
                {
                    Application.Current.Dispatcher.Invoke(() => { ScanResultText += message + "\n"; });
                    StatusMessage = message;
                    if (gameInfo != null && gameId > 0) { UpdateGamesCollection(gameInfo, gameId); }
                };
                await ScanProcessHelper.ProcessFolderScanAsync(folderPath, scannedGames, _folderScannerService, _webMetadataService, _databaseService, progressCallback, token);
            }
            catch (OperationCanceledException)
            {
                caughtException = null;
                string cancelMessage = "사용자 요청에 의해 스캔이 중지되었습니다.";
                ScanResultText += cancelMessage + "\n";
                StatusMessage = cancelMessage;
                MessageBox.Show(cancelMessage, "스캔 중지됨", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                string errorMessage = $"스캔 처리 중 심각한 오류 발생: {ex.Message}";
                ScanResultText += errorMessage + "\n";
                StatusMessage = "스캔 오류 발생";
                MessageBox.Show($"폴더 스캔 처리 중 오류 발생: {ex.Message}", "스캔 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (scannedGames.Any() && caughtException == null && !token.IsCancellationRequested)
                {
                    string completeMessage = $"폴더 스캔 완료: {scannedGames.Count}개 게임 발견";
                    StatusMessage = completeMessage;
                    MessageBox.Show($"폴더 스캔이 완료되었습니다. {scannedGames.Count}개의 게임을 발견했습니다.", "폴더 스캔 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (!scannedGames.Any() && caughtException == null && !string.IsNullOrEmpty(folderPath) && !token.IsCancellationRequested)
                {
                    StatusMessage = "스캔 완료: 게임 없음";
                    MessageBox.Show("지정된 폴더에서 인식 가능한 게임 정보를 찾지 못했습니다.", "폴더 스캔 결과", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                await LoadGamesToGridAsync();
                IsScanning = false;
                _scanCancellationTokenSource?.Dispose();
                _scanCancellationTokenSource = null;
            }
        }

        private async Task ShowGameInfo(GameInfo? game)
        {
            if (game == null) return;
            var gameDetails = await _databaseService.GetGameAsync(game.Identifier);
            if (gameDetails == null)
            {
                MessageBox.Show("선택된 게임의 상세 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var infoDialog = new GameInfoDialog(gameDetails) { Owner = Application.Current.MainWindow };
            if (infoDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(infoDialog.SearchTerm))
                {
                    SearchField = infoDialog.SearchCategory;
                    SearchText = infoDialog.SearchTerm;
                }
                else if (infoDialog.Game != null)
                {
                    await _databaseService.UpdateGameAsync(infoDialog.Game);
                    await LoadGamesToGridAsync();
                    StatusMessage = "게임 정보 업데이트 성공";
                }
            }
        }

        private void RunGame(GameInfo game)
        {
            if (game == null) { MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            StatusMessage = $"{game.Title} 실행 중...";
            bool success = GameExecutionHelper.RunGame(game, Application.Current.MainWindow);
            StatusMessage = success ? $"{game.Title} 실행됨" : "게임 실행 실패";
        }

        private void OpenFolder(GameInfo game)
        {
            if (game == null || string.IsNullOrEmpty(game.FolderPath)) { MessageBox.Show("폴더 경로를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            try
            {
                StatusMessage = $"{game.Title} 폴더 열기 중...";
                var psi = new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{game.FolderPath}\"", UseShellExecute = true };
                Process.Start(psi);
                StatusMessage = $"{game.Title} 폴더 열림";
            }
            catch (Exception ex)
            {
                StatusMessage = "폴더 열기 실패";
                MessageBox.Show($"폴더를 여는 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteGameUiAsync(GameInfo? game)
        {
            if (game == null) { MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            var result = MessageBox.Show($"정말로 '{game.Title}' 게임을 데이터베이스에서 삭제하시겠습니까?\n\n이 작업은 취소할 수 없습니다.", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusMessage = $"{game.Title} 삭제 중...";
                    await _databaseService.DeleteGameAsync(game.Identifier);
                    await LoadGamesToGridAsync();
                    StatusMessage = $"{game.Title} 삭제 완료";
                    MessageBox.Show("게임이 성공적으로 삭제되었습니다.", "삭제 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = "게임 삭제 오류";
                    MessageBox.Show($"게임 삭제 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ClearDatabaseUiAsync()
        {
            var result = MessageBox.Show("정말로 데이터베이스를 초기화하시겠습니까?\n\n이 작업은 모든 게임 정보를 삭제하며 취소할 수 없습니다.", "데이터베이스 초기화 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var secondResult = MessageBox.Show("정말 확실합니까? 모든 데이터가 삭제됩니다.", "최종 확인", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (secondResult == MessageBoxResult.Yes)
                    {
                        StatusMessage = "데이터베이스 초기화 중...";
                        await _databaseService.ClearAllGamesAsync();
                        CurrentPage = 1;
                        await LoadGamesToGridAsync();
                        StatusMessage = "데이터베이스 초기화 완료: 모든 항목 삭제됨";
                        MessageBox.Show($"데이터베이스가 초기화되었습니다. 모든 항목이 삭제되었습니다.", "초기화 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = "데이터베이스 초기화 오류";
                    MessageBox.Show($"데이터베이스 초기화 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ToggleTheme()
        {
            ThemeManager.ToggleTheme();
            CurrentTheme = ThemeManager.CurrentTheme;
            StatusMessage = $"테마 변경: {CurrentTheme} 모드";
            _settingsService.Settings.CurrentTheme = CurrentTheme;
            _settingsService.SaveSettings();
        }

        private void ExitApplication() { Application.Current.Shutdown(); }

        private void ShowAboutInfo() { MessageBox.Show("DL Game Viewer v1.0\n\n© 2024 All rights reserved.", "프로그램 정보", MessageBoxButton.OK, MessageBoxImage.Information); }

        private async Task RefreshGameDataAsync(GameInfo? game)
        {
            if (game == null)
            {
                MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                StatusMessage = $"{game.Title} 데이터 갱신 중...";

                // 웹에서 새로운 메타데이터 가져오기
                GameInfo updatedInfo = await _webMetadataService.FetchMetadataAsync(game.Identifier);

                // 기존 데이터와 병합 (로컬 정보는 유지)
                updatedInfo.Id = game.Id;
                updatedInfo.FolderPath = game.FolderPath;
                updatedInfo.SaveFolderPath = game.SaveFolderPath;
                updatedInfo.ExecutableFiles = game.ExecutableFiles;
                updatedInfo.DateAdded = game.DateAdded;
                updatedInfo.LastPlayed = game.LastPlayed;
                updatedInfo.PlayTime = game.PlayTime;
                updatedInfo.FileSizeInBytes = game.FileSizeInBytes;
                updatedInfo.UserMemo = game.UserMemo;
                updatedInfo.CoverImagePath = game.CoverImagePath; // 로컬 커버 이미지 경로 유지

                // 데이터베이스 업데이트
                await _databaseService.UpdateGameAsync(updatedInfo);

                // UI에서 직접 항목 업데이트
                var gameInCollection = Games.FirstOrDefault(g => g.Id == game.Id);
                if (gameInCollection != null)
                {
                    // 속성을 하나씩 업데이트하여 UI에 변경 알림
                    gameInCollection.Title = updatedInfo.Title;
                    gameInCollection.Creator = updatedInfo.Creator;
                    gameInCollection.GameType = updatedInfo.GameType;
                    gameInCollection.Genres = updatedInfo.Genres;
                    gameInCollection.SalesCount = updatedInfo.SalesCount;
                    gameInCollection.Rating = updatedInfo.Rating;
                    gameInCollection.RatingCount = updatedInfo.RatingCount;
                    gameInCollection.ReleaseDate = updatedInfo.ReleaseDate;
                    // CoverImagePath는 웹에서 새로 받아오지 않으므로 업데이트하지 않음
                }

                StatusMessage = $"{game.Title} 데이터 갱신 성공";
                MessageBox.Show($"{game.Title} 게임의 메타데이터가 성공적으로 갱신되었습니다.", "갱신 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "데이터 갱신 실패";
                MessageBox.Show($"데이터 갱신 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // IsLoading을 사용하지 않으므로 관련 코드 없음
            }
        }

        private void OpenDlSite(GameInfo? game)
        {
            if (game == null || string.IsNullOrEmpty(game.Identifier)) { MessageBox.Show("유효한 게임 식별자를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            try
            {
                StatusMessage = $"{game.Title} DLsite 페이지 열기 중...";
                string url = game.Identifier.StartsWith("VJ") ? $"https://www.dlsite.com/pro/work/=/product_id/{game.Identifier}.html/?locale=ko_KR" : $"https://www.dlsite.com/maniax/work/=/product_id/{game.Identifier}.html/?locale=ko_KR";
                var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
                Process.Start(psi);
                StatusMessage = $"{game.Title} DLsite 페이지 열림";
            }
            catch (Exception ex)
            {
                StatusMessage = "웹페이지 열기 실패";
                MessageBox.Show($"DLsite 페이지를 여는 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSaveFolder(GameInfo? game)
        {
            if (game == null || string.IsNullOrEmpty(game.SaveFolderPath)) { MessageBox.Show("세이브 폴더 경로를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            try
            {
                StatusMessage = $"{game.Title} 세이브 폴더 열기 중...";
                if (!Directory.Exists(game.SaveFolderPath))
                {
                    MessageBox.Show($"세이브 폴더가 존재하지 않습니다: {game.SaveFolderPath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "세이브 폴더 없음";
                    return;
                }
                var psi = new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{game.SaveFolderPath}\"", UseShellExecute = true };
                Process.Start(psi);
                StatusMessage = $"{game.Title} 세이브 폴더 열림";
            }
            catch (Exception ex)
            {
                StatusMessage = "세이브 폴더 열기 실패";
                MessageBox.Show($"세이브 폴더를 여는 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}