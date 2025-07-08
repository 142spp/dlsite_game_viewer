using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DLGameViewer.Models;

namespace DLGameViewer.ViewModels
{
    public class GameInfoViewModel : ViewModelBase
    {
        private GameInfo _game;
        private string _identifier;
        private string _title;
        private string _creator;
        private string _genres;
        private string _rating;
        private string _coverImageUrl;
        private string _localImagePath;
        private string _folderPath;
        private string _executableFiles;
        private string _userMemo;
        private string _displayedFullImagePath;
        private int _selectedPreviewIndex = -1;
        private string _clipboardMessage;
        private DispatcherTimer _messageTimer;

        // 추가된 메타데이터 필드
        private string _gameType;
        private string _releaseDate;
        private string _salesCount;
        private string _ratingCountDisplay; // RatingCount는 이미 GameInfo에 int로 있으므로, 표시용 문자열 속성
        private string _fileSize;
        private string _saveFolderPath;
        private string _dateAdded;
        private string _lastPlayed;
        private string _playTime;

        private const double MinRating = 0.00;
        private const double MaxRating = 5.00;
        private const double RatingStep = 0.10;

        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Creator
        {
            get => _creator;
            set => SetProperty(ref _creator, value);
        }

        public string Genres
        {
            get => _genres;
            set => SetProperty(ref _genres, value);
        }

        public string Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        public string CoverImageUrl
        {
            get => _coverImageUrl;
            set => SetProperty(ref _coverImageUrl, value);
        }

        public string LocalImagePath
        {
            get => _localImagePath;
            set => SetProperty(ref _localImagePath, value);
        }

        public string FolderPath
        {
            get => _folderPath;
            set => SetProperty(ref _folderPath, value);
        }

        public string ExecutableFiles
        {
            get => _executableFiles;
            set => SetProperty(ref _executableFiles, value);
        }

        public string UserMemo
        {
            get => _userMemo;
            set => SetProperty(ref _userMemo, value);
        }

        // 추가된 메타데이터 속성
        public string GameType { get => _gameType; set => SetProperty(ref _gameType, value); }
        public string ReleaseDate { get => _releaseDate; set => SetProperty(ref _releaseDate, value); }
        public string SalesCount { get => _salesCount; set => SetProperty(ref _salesCount, value); }
        public string RatingCountDisplay { get => _ratingCountDisplay; set => SetProperty(ref _ratingCountDisplay, value); }
        public string FileSize { get => _fileSize; set => SetProperty(ref _fileSize, value); }
        public string SaveFolderPath { get => _saveFolderPath; set => SetProperty(ref _saveFolderPath, value); }
        public string DateAdded { get => _dateAdded; set => SetProperty(ref _dateAdded, value); }
        public string LastPlayed { get => _lastPlayed; set => SetProperty(ref _lastPlayed, value); }
        public string PlayTime { get => _playTime; set => SetProperty(ref _playTime, value); }

        public string DisplayedFullImagePath
        {
            get => _displayedFullImagePath;
            set => SetProperty(ref _displayedFullImagePath, value);
        }

        public int SelectedPreviewIndex
        {
            get => _selectedPreviewIndex;
            set
            {
                if (SetProperty(ref _selectedPreviewIndex, value) && value >= 0 && value < PreviewImageFilePaths.Count)
                {
                    DisplayedFullImagePath = PreviewImageFilePaths[value];
                }
            }
        }

        public ObservableCollection<string> PreviewImageFilePaths { get; } = new ObservableCollection<string>();

        public string ClipboardMessage
        {
            get => _clipboardMessage;
            set => SetProperty(ref _clipboardMessage, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RatingUpCommand { get; }
        public ICommand RatingDownCommand { get; }
        public ICommand SelectImageCommand { get; }
        public ICommand CopyIdentifierCommand { get; }
        public ICommand SearchTagCommand { get; }

        public event EventHandler<bool> RequestClose;
        public event Action<string, string> SearchRequested;

        public List<string> GenreList { get; private set; }

        public GameInfoViewModel(GameInfo game)
        {
            _game = game ?? new GameInfo();
            
            // 속성 초기화
            LoadGameInfo();
            LoadPreviewImages();
            ClipboardMessage = string.Empty;
            
            // 명령 초기화
            SaveCommand = CreateCommand(param => ExecuteSave());
            CancelCommand = CreateCommand(param => ExecuteCancel());
            RatingUpCommand = CreateCommand(param => ExecuteRatingUp());
            RatingDownCommand = CreateCommand(param => ExecuteRatingDown());
            SelectImageCommand = CreateCommand(param => ExecuteSelectImage(param));
            CopyIdentifierCommand = CreateCommand(param => ExecuteCopyIdentifier());
            SearchTagCommand = new RelayCommand(param => ExecuteSearchTag(param));

            // 메시지 타이머 초기화
            _messageTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _messageTimer.Tick += (s, e) =>
            {
                ClipboardMessage = string.Empty;
                _messageTimer.Stop();
            };
        }

        private void LoadGameInfo()
        {
            Identifier = _game.Identifier;
            Title = _game.Title;
            Creator = _game.Creator;
            Genres = string.Join(", ", _game.Genres ?? new List<string>());
            GenreList = _game.Genres?.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).ToList() ?? new List<string>();
            
            double currentRating = MinRating;
            if (double.TryParse(_game.Rating, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedRating))
            {
                currentRating = Math.Clamp(parsedRating, MinRating, MaxRating);
            }
            Rating = currentRating.ToString("N2", CultureInfo.InvariantCulture);
            
            CoverImageUrl = _game.CoverImageUrl;
            LocalImagePath = _game.LocalImagePath;
            FolderPath = _game.FolderPath;
            ExecutableFiles = string.Join("\n", _game.ExecutableFiles ?? new List<string>());
            UserMemo = _game.UserMemo;

            // 추가된 메타데이터 로드
            GameType = _game.GameType;
            ReleaseDate = _game.ReleaseDate?.ToString("yyyy-MM-dd") ?? "N/A";
            SalesCount = _game.SalesCount.ToString("N0", CultureInfo.InvariantCulture); // 숫자 형식 (예: 1,234)
            RatingCountDisplay = _game.RatingCount.ToString("N0", CultureInfo.InvariantCulture);
            FileSize = _game.FileSize;
            SaveFolderPath = _game.SaveFolderPath;
            DateAdded = _game.DateAdded.ToString("yyyy-MM-dd HH:mm:ss");
            LastPlayed = _game.LastPlayed?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
            PlayTime = _game.PlayTime.ToString(@"hh\:mm\:ss");
            
            // 현재 표시할 이미지 설정
            DisplayedFullImagePath = _game.CoverImagePath;
        }

        private void LoadPreviewImages()
        {
            PreviewImageFilePaths.Clear();
            
            if (!string.IsNullOrWhiteSpace(_game.LocalImagePath) && Directory.Exists(_game.LocalImagePath))
            {
                var imageExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                
                try
                {
                    var files = Directory.EnumerateFiles(_game.LocalImagePath)
                        .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
                    
                    foreach (var file in files)
                    {
                        PreviewImageFilePaths.Add(file);
                    }
                }
                catch (Exception)
                {
                    // 폴더 접근 오류 등 예외 처리
                }
            }
        }

        private void ExecuteSave()
        {
            // ViewModel에서 Model로 데이터 업데이트
            _game.Title = Title;
            _game.Creator = Creator;
            _game.Rating = Rating;
            _game.CoverImageUrl = CoverImageUrl;
            // CoverImagePath, LocalImagePath는 여기서 변경하지 않음
            _game.UserMemo = UserMemo;

            // 추가된 메타데이터 저장 (편집 가능한 경우)
            _game.GameType = GameType; 
            // ReleaseDate, SalesCount, RatingCount 등은 보통 웹에서 가져오므로 직접 저장 로직은 제외 (필요시 추가)
            _game.FileSize = FileSize; 
            _game.SaveFolderPath = SaveFolderPath;
            // DateAdded, LastPlayed, PlayTime 등은 시스템 관리 필드이므로 직접 저장 로직은 제외
            
            RequestClose?.Invoke(this, true);
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(this, false);
        }

        private void ExecuteRatingUp()
        {
            if (double.TryParse(Rating, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentValue))
            {
                double newValue = currentValue + RatingStep;
                if (newValue > MaxRating) newValue = MaxRating;
                newValue = Math.Round(newValue, 2);
                Rating = newValue.ToString("N2", CultureInfo.InvariantCulture);
            }
        }

        private void ExecuteRatingDown()
        {
            if (double.TryParse(Rating, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentValue))
            {
                double newValue = currentValue - RatingStep;
                if (newValue < MinRating) newValue = MinRating;
                newValue = Math.Round(newValue, 2);
                Rating = newValue.ToString("N2", CultureInfo.InvariantCulture);
            }
        }

        private void ExecuteSelectImage(object parameter)
        {
            if (parameter is string imagePath)
            {
                DisplayedFullImagePath = imagePath;
            }
        }

        private void ExecuteCopyIdentifier()
        {
            if (!string.IsNullOrEmpty(Identifier))
            {
                try
                {
                    // 클립보드에 식별자 복사
                    System.Windows.Clipboard.SetText(Identifier);
                    
                    // 성공 메시지 표시
                    ClipboardMessage = "클립보드에 저장되었습니다";
                    
                    // 3초 후 메시지 사라지게 하기
                    _messageTimer.Stop();
                    _messageTimer.Start();
                }
                catch (Exception ex)
                {
                    // 클립보드 액세스 실패 시 예외 처리
                    System.Windows.MessageBox.Show($"클립보드에 복사하는 중 오류가 발생했습니다: {ex.Message}", 
                        "클립보드 오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    
                    // 오류 메시지 표시
                    ClipboardMessage = "복사 실패";
                    
                    // 3초 후 메시지 사라지게 하기
                    _messageTimer.Stop();
                    _messageTimer.Start();
                }
            }
        }
        
        public GameInfo GetUpdatedGame()
        {
            return _game;
        }

        private void ExecuteSearchTag(object parameter)
        {
            if (parameter is object[] values && values.Length == 2)
            {
                string category = values[0] as string;
                string term = values[1] as string;

                if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(term))
                {
                    SearchRequested?.Invoke(category, term);
                }
            }
        }
    }
} 