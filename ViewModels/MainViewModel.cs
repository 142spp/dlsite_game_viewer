using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using DLGameViewer.Dialogs;
using DLGameViewer.Helpers;
using DLGameViewer.Interfaces;
using DLGameViewer.Models;
using Microsoft.Win32;

namespace DLGameViewer.ViewModels {
    public class MainViewModel : ViewModelBase {
        private readonly IDatabaseService _databaseService;
        private readonly IFolderScannerService _folderScannerService;
        private readonly IWebMetadataService _webMetadataService;
        private readonly IImageService _imageService;
        private bool _isLoading;
        private ThemeType _currentTheme;
        private GameInfo _selectedGame;
        private string _searchText;
        private string _statusMessage;
        private string _sortField = "Title"; // 기본 정렬 필드
        private bool _isAscending = true;    // 기본 정렬 방향

        public ObservableCollection<GameInfo> Games { get; private set; }

        public bool IsLoading {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ThemeType CurrentTheme {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        public GameInfo SelectedGame {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
        }

        public string SearchText {
            get => _searchText;
            set {
                if (SetProperty(ref _searchText, value)) {
                    // 검색 텍스트가 변경되면 필터 적용
                    ApplyFilter();
                }
            }
        }

        public string StatusMessage {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SortField {
            get => _sortField;
            set {
                try {
                    if (SetProperty(ref _sortField, value)) {
                        // 정렬 필드가 변경되면 정렬 적용
                        ApplySorting();
                    }
                } catch (Exception ex) {
                    StatusMessage = $"정렬 필드 변경 중 오류: {ex.Message}";
                    MessageBox.Show($"정렬 필드 변경 중 오류가 발생했습니다: {ex.Message}", "정렬 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public bool IsAscending {
            get => _isAscending;
            set {
                if (SetProperty(ref _isAscending, value)) {
                    // 정렬 방향이 변경되면 정렬 적용
                    ApplySorting();
                }
            }
        }

        // 명령들
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

        public MainViewModel(
            IDatabaseService databaseService,
            IFolderScannerService folderScannerService,
            IWebMetadataService webMetadataService,
            IImageService imageService) {
            _databaseService = databaseService;
            _folderScannerService = folderScannerService;
            _webMetadataService = webMetadataService;
            _imageService = imageService;

            Games = new ObservableCollection<GameInfo>();
            CurrentTheme = ThemeManager.CurrentTheme;
            SearchText = string.Empty;
            StatusMessage = "준비";

            // 명령 초기화
            ScanFolderCommand = new RelayCommand(async param => await ScanFolderAsync());
            RefreshGameListCommand = new RelayCommand(param => LoadGamesToGrid());
            GameInfoCommand = new RelayCommand(param => ShowGameInfo(param as GameInfo));
            RunGameCommand = new RelayCommand(param => RunGame(param as GameInfo));
            OpenFolderCommand = new RelayCommand(param => OpenFolder(param as GameInfo));
            DeleteGameCommand = new RelayCommand(param => DeleteGame(param as GameInfo));
            ClearDatabaseCommand = new RelayCommand(param => ClearDatabase());
            ToggleThemeCommand = new RelayCommand(param => ToggleTheme());
            ExitApplicationCommand = new RelayCommand(param => ExitApplication());
            AboutCommand = new RelayCommand(param => ShowAboutInfo());
            ClearSearchCommand = new RelayCommand(param => ClearSearch());
            SortCommand = new RelayCommand(param => Sort(param as string));

            // 초기 로드
            LoadGamesToGrid();
        }

        // 데이터를 로드하여 게임 목록 그리드에 표시
        private void LoadGamesToGrid() {
            IsLoading = true;
            StatusMessage = "게임 목록 로드 중...";
            Games.Clear();

            try {
                var gamesFromDb = _databaseService.GetAllGames();
                if (gamesFromDb != null) {
                    foreach (var game in gamesFromDb) {
                        Games.Add(game);
                    }
                    StatusMessage = $"{Games.Count}개 게임 로드됨";

                    // 로딩 후 정렬 적용
                    ApplySorting();
                }
            } catch (Exception ex) {
                StatusMessage = "오류 발생";
                MessageBox.Show($"게임 목록을 불러오는 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                IsLoading = false;
            }
        }

        // 폴더 스캔 명령 처리
        private async Task ScanFolderAsync() {
            IsLoading = true;
            StatusMessage = "폴더 스캔 중...";

            // 스캔 결과 대화상자 생성 및 표시
            var scanResultDialog = new ScanResultDialog();
            scanResultDialog.Owner = Application.Current.MainWindow;

            List<GameInfo> scannedGames = new List<GameInfo>();
            Exception? caughtException = null;
            string folderPath = string.Empty;

            try {
                // 폴더 선택 다이얼로그 표시
                var dialog = new OpenFolderDialog {
                    Title = "스캔할 폴더를 선택하세요"
                };

                if (dialog.ShowDialog() != true) {
                    scanResultDialog.AppendText("폴더 선택이 취소되었습니다.");
                    StatusMessage = "폴더 스캔 취소됨";
                    return;
                }

                scanResultDialog.Show(); // 비모달로 표시

                folderPath = dialog.FolderName;
                scanResultDialog.AppendText($"선택한 폴더: {folderPath}");
                scanResultDialog.AppendText("스캔을 시작합니다...");

                // 콜백 정의: 실시간 진행 상황 업데이트를 위한 함수
                ScanProcessHelper.ProgressUpdateCallback progressCallback = (message, gameInfo, gameId) => {
                    // 대화상자에 결과를 표시
                    scanResultDialog.AppendText(message);

                    // 상태 메시지 업데이트
                    StatusMessage = message;

                    // 새 게임이 추가된 경우 UI 업데이트
                    if (gameInfo != null && gameId > 0) {
                        UpdateGamesCollection(gameInfo, gameId);
                    }
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

                // 스캔 완료 후 게임 목록 갱신
                if (scannedGames.Any()) {
                    LoadGamesToGrid();
                }
            } catch (Exception ex) {
                caughtException = ex;
                string errorMessage = $"스캔 처리 중 심각한 오류 발생: {ex.Message}";
                scanResultDialog.AppendText(errorMessage);
                StatusMessage = "스캔 오류 발생";
                MessageBox.Show($"폴더 스캔 처리 중 오류 발생: {ex.Message}", "스캔 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                // 스캔이 성공적으로 완료되면 메시지 표시
                if (scannedGames.Any() && caughtException == null) {
                    string completeMessage = $"폴더 스캔 완료: {scannedGames.Count}개 게임 발견";
                    StatusMessage = completeMessage;
                    MessageBox.Show($"폴더 스캔이 완료되었습니다. {scannedGames.Count}개의 게임을 발견했습니다.",
                                  "폴더 스캔 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                } else if (!scannedGames.Any() && caughtException == null && !string.IsNullOrEmpty(folderPath)) {
                    StatusMessage = "스캔 완료: 게임 없음";
                    MessageBox.Show("지정된 폴더에서 인식 가능한 게임 정보를 찾지 못했습니다.",
                                  "폴더 스캔 결과", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsLoading = false;
            }
        }

        // 기존 목록을 유지하면서 새 게임 추가
        private void UpdateGamesCollection(GameInfo gameInfo, long gameId) {
            gameInfo.Id = gameId; // ID 설정

            // UI 스레드에서 실행 (필요한 경우)
            Application.Current.Dispatcher.Invoke(() => {
                // 중복 방지
                if (!Games.Any(g => g.Id == gameId)) {
                    Games.Add(gameInfo);
                }
            });
        }

        // 게임 정보 표시
        private void ShowGameInfo(GameInfo game) {
            if (game == null) {
                MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // GameInfoDialog 생성
            GameInfoDialog infoDialog = new GameInfoDialog(game) {
                Owner = Application.Current.MainWindow
            };

            if (infoDialog.ShowDialog() == true) {
                try {
                    int updatedRows = _databaseService.UpdateGame(infoDialog.Game);
                    if (updatedRows > 0) {
                        LoadGamesToGrid(); // UI 새로고침
                        StatusMessage = "게임 정보 업데이트 성공";
                        MessageBox.Show("게임 정보가 성공적으로 업데이트되었습니다.", "정보 업데이트", MessageBoxButton.OK, MessageBoxImage.Information);
                    } else {
                        StatusMessage = "게임 정보 업데이트 실패";
                        MessageBox.Show("게임 정보 업데이트에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                } catch (Exception ex) {
                    StatusMessage = "게임 정보 업데이트 오류";
                    MessageBox.Show($"게임 정보 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 게임 실행
        private void RunGame(GameInfo game) {
            if (game == null) {
                MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StatusMessage = $"{game.Title} 실행 중...";

            // 게임 실행 로직을 Helper 클래스로 위임
            bool success = GameExecutionHelper.RunGame(game, Application.Current.MainWindow);

            if (success) {
                StatusMessage = $"{game.Title} 실행됨";
            } else {
                StatusMessage = "게임 실행 실패";
            }
        }

        // 폴더 열기
        private void OpenFolder(GameInfo game) {
            if (game == null || string.IsNullOrEmpty(game.FolderPath)) {
                MessageBox.Show("폴더 경로를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try {
                StatusMessage = $"{game.Title} 폴더 열기 중...";

                // 폴더 열기 - 특수 문자 처리를 위해 ProcessStartInfo 사용
                var psi = new System.Diagnostics.ProcessStartInfo {
                    FileName = "explorer.exe",
                    Arguments = $"\"{game.FolderPath}\"", // 따옴표로 경로 감싸기
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                StatusMessage = $"{game.Title} 폴더 열림";
            } catch (Exception ex) {
                StatusMessage = "폴더 열기 실패";
                MessageBox.Show($"폴더를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 게임 삭제
        private void DeleteGame(GameInfo game) {
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
                    StatusMessage = $"{game.Title} 삭제 중...";
                    int deletedRows = _databaseService.DeleteGame(game.Id);
                    if (deletedRows > 0) {
                        LoadGamesToGrid(); // UI 새로고침
                        StatusMessage = "게임 삭제 완료";
                        MessageBox.Show("게임이 성공적으로 삭제되었습니다.", "삭제 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                    } else {
                        StatusMessage = "게임 삭제 실패";
                        MessageBox.Show("게임 삭제에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                } catch (Exception ex) {
                    StatusMessage = "게임 삭제 오류";
                    MessageBox.Show($"게임 삭제 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 데이터베이스 초기화
        private void ClearDatabase() {
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
                        StatusMessage = "데이터베이스 초기화 중...";

                        // 데이터베이스 초기화 실행
                        _databaseService.ResetDatabase(); // 디버그용
                        int deletedRows = _databaseService.ClearAllGames();

                        // UI 갱신
                        LoadGamesToGrid();

                        StatusMessage = $"데이터베이스 초기화 완료: {deletedRows}개 항목 삭제됨";

                        // 결과 메시지 표시
                        MessageBox.Show(
                            $"데이터베이스가 초기화되었습니다. {deletedRows}개의 항목이 삭제되었습니다.",
                            "초기화 완료",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                } catch (Exception ex) {
                    StatusMessage = "데이터베이스 초기화 오류";
                    MessageBox.Show(
                        $"데이터베이스 초기화 중 오류가 발생했습니다: {ex.Message}",
                        "오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // 테마 전환
        private void ToggleTheme() {
            ThemeManager.ToggleTheme();
            CurrentTheme = ThemeManager.CurrentTheme;
            StatusMessage = $"테마 변경: {CurrentTheme} 모드";
        }

        // 애플리케이션 종료
        private void ExitApplication() {
            Application.Current.Shutdown();
        }

        // 정보 대화상자 표시
        private void ShowAboutInfo() {
            MessageBox.Show("DL Game Viewer v1.0\n\n© 2024 All rights reserved.",
                           "프로그램 정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 검색 필터 적용
        private void ApplyFilter() {
            if (string.IsNullOrWhiteSpace(SearchText)) {
                LoadGamesToGrid(); // 검색어가 없으면 전체 목록 표시
                return;
            }

            IsLoading = true;
            StatusMessage = "검색 필터 적용 중...";

            try {
                // 데이터베이스에서 검색어에 맞는 게임 가져오기
                var filteredGames = _databaseService.SearchGames(SearchText);

                // UI 업데이트
                Games.Clear();
                foreach (var game in filteredGames) {
                    Games.Add(game);
                }

                // 필터 적용 후 정렬도 적용
                ApplySorting();

                StatusMessage = $"검색 결과: {Games.Count}개 항목 발견";
            } catch (Exception ex) {
                StatusMessage = "검색 오류";
                MessageBox.Show($"검색 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                IsLoading = false;
            }
        }

        // 검색어 초기화
        private void ClearSearch() {
            SearchText = string.Empty;
            LoadGamesToGrid();
        }

        // 정렬 필드 설정 명령
        private void Sort(string field) {
            try {
                if (string.IsNullOrEmpty(field)) return;

                // 같은 필드를 다시 선택한 경우 정렬 방향 토글
                if (field == SortField) {
                    IsAscending = !IsAscending;
                } else {
                    // 다른 필드를 선택한 경우 해당 필드로 변경하고 오름차순 설정
                    SortField = field;
                    IsAscending = true;
                }
            } catch (Exception ex) {
                StatusMessage = $"정렬 명령 처리 중 오류: {ex.Message}";
                MessageBox.Show($"정렬 명령 처리 중 오류가 발생했습니다: {ex.Message}", "정렬 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 현재 설정된 필드와 방향으로 정렬 적용
        private void ApplySorting() {
            if (Games == null || !Games.Any()) return;

            IOrderedEnumerable<GameInfo> sortedGames;

            try {
                switch (SortField) {
                    case "Title":
                        sortedGames = IsAscending ? Games.OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase) 
                                                  : Games.OrderByDescending(g => g.Title, StringComparer.CurrentCultureIgnoreCase);
                        break;
                    case "Identifier":
                        sortedGames = IsAscending ? Games.OrderBy(g => g.Identifier) : Games.OrderByDescending(g => g.Identifier);
                        break;
                    case "Creator":
                        sortedGames = IsAscending ? Games.OrderBy(g => g.Creator, StringComparer.CurrentCultureIgnoreCase) 
                                                  : Games.OrderByDescending(g => g.Creator, StringComparer.CurrentCultureIgnoreCase);
                        break;
                    case "Rating": // Rating은 문자열이므로, 숫자처럼 비교하려면 변환 필요. 현재는 문자열 비교.
                        sortedGames = IsAscending ? Games.OrderBy(g => g.Rating) : Games.OrderByDescending(g => g.Rating);
                        break;
                    case "DateAdded":
                        sortedGames = IsAscending ? Games.OrderBy(g => g.DateAdded) : Games.OrderByDescending(g => g.DateAdded);
                        break;
                    case "LastPlayed":
                        sortedGames = IsAscending 
                            ? Games.OrderBy(g => g.LastPlayed.HasValue).ThenBy(g => g.LastPlayed ?? DateTime.MinValue) 
                            : Games.OrderByDescending(g => g.LastPlayed.HasValue).ThenByDescending(g => g.LastPlayed ?? DateTime.MaxValue);
                        break;
                    case "ReleaseDate":
                        sortedGames = IsAscending 
                            ? Games.OrderBy(g => g.ReleaseDate.HasValue).ThenBy(g => g.ReleaseDate ?? DateTime.MinValue) 
                            : Games.OrderByDescending(g => g.ReleaseDate.HasValue).ThenByDescending(g => g.ReleaseDate ?? DateTime.MaxValue);
                        break;
                    case "Genres":
                        sortedGames = IsAscending 
                            ? Games.OrderBy(g => !g.Genres.Any()).ThenBy(g => g.Genres.FirstOrDefault()) 
                            : Games.OrderByDescending(g => g.Genres.Any()).ThenByDescending(g => g.Genres.FirstOrDefault());
                        break;
                    case "FileSize":
                        sortedGames = IsAscending ? Games.OrderBy(g => g.FileSize) : Games.OrderByDescending(g => g.FileSize);
                        break;
                    case "PlayTime":
                        sortedGames = IsAscending ? Games.OrderBy(g => g.PlayTime) : Games.OrderByDescending(g => g.PlayTime);
                        break;
                    default:
                        // 기본 정렬 또는 오류 처리
                        sortedGames = Games.OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase);
                        StatusMessage = $"알 수 없는 정렬 필드: {SortField}. 제목으로 정렬합니다.";
                        break;
                }
                Games = new ObservableCollection<GameInfo>(sortedGames);
                // GamesView.Refresh(); // CollectionViewSource를 사용하는 경우 필요할 수 있음
                // CollectionViewSource를 사용하지 않고 직접 ObservableCollection을 교체했으므로, 
                // 바인딩된 컨트롤은 자동으로 업데이트됨. 만약 업데이트 안되면 NotifyPropertyChanged("Games") 필요.
                OnPropertyChanged(nameof(Games)); // ObservableCollection 자체를 교체했으므로 알림

            } catch (Exception ex) {
                StatusMessage = $"게임 정렬 중 오류 발생: {ex.Message}";
                 MessageBox.Show($"게임 정렬 중 오류가 발생했습니다: {ex.Message}", "정렬 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                // 오류 발생 시 기본 정렬 (예: 제목 오름차순)
                var defaultSorted = Games.OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase);
                Games = new ObservableCollection<GameInfo>(defaultSorted);
                OnPropertyChanged(nameof(Games));
            }
        }
    }
}