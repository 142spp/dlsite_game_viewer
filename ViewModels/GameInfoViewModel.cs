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

        public event EventHandler<bool> RequestClose;

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
            
            double currentRating = MinRating;
            if (double.TryParse(_game.Rating, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedRating))
            {
                currentRating = Math.Clamp(parsedRating, MinRating, MaxRating);
            }
            Rating = currentRating.ToString("N1", CultureInfo.InvariantCulture);
            
            CoverImageUrl = _game.CoverImageUrl;
            LocalImagePath = _game.LocalImagePath;
            FolderPath = _game.FolderPath;
            ExecutableFiles = string.Join("\n", _game.ExecutableFiles ?? new List<string>());
            UserMemo = _game.UserMemo;
            
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
            _game.Genres = Genres.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim()).ToList();
            _game.Rating = Rating;
            _game.CoverImageUrl = CoverImageUrl;
            // CoverImagePath, LocalImagePath는 여기서 변경하지 않음
            _game.UserMemo = UserMemo;
            
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
    }
} 