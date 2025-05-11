using DLGameViewer.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Globalization; // CultureInfo 사용을 위해 추가
using System.ComponentModel; // INotifyPropertyChanged
using System.Runtime.CompilerServices; // CallerMemberName
using System.IO; // Path
using System.Collections.Generic; // List
using System.Collections.ObjectModel; // ObservableCollection

namespace DLGameViewer.Dialogs {
    public partial class GameInfoDialog : Window, INotifyPropertyChanged // INotifyPropertyChanged 구현
    {
        public GameInfo Game { get; private set; }

        private string? _displayedFullImagePath;
        public string? DisplayedFullImagePath {
            get => _displayedFullImagePath;
            set {
                if (_displayedFullImagePath != value) {
                    _displayedFullImagePath = value;
                    OnPropertyChanged(); // UI에 변경 알림
                }
            }
        }

        // 미리보기에 사용할 이미지 파일들의 "절대 경로" 리스트
        public ObservableCollection<string> PreviewImageFilePaths { get; private set; }

        private const double MinRating = 0.00;
        private const double MaxRating = 5.00;
        private const double RatingStep = 0.10;

        public GameInfoDialog(GameInfo gameInfo) {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Game = gameInfo;
            PreviewImageFilePaths = new ObservableCollection<string>();
            this.DataContext = this; 
            LoadGameInfoAndPreviewImages();
        }

        private void LoadGameInfoAndPreviewImages() {
            IdentifierTextBlock.Text = Game.Identifier;
            TitleTextBox.Text = Game.Title;
            CreatorTextBox.Text = Game.Creator;
            GenresTextBox.Text = string.Join(", ", Game.Genres ?? new List<string>());
            
            double currentRating = MinRating; 
            if (double.TryParse(Game.Rating, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedRating)) {
                currentRating = Math.Clamp(parsedRating, MinRating, MaxRating);
            }
            RatingTextBox.Text = currentRating.ToString("N1", CultureInfo.InvariantCulture); 

            CoverImageUrlTextBox.Text = Game.CoverImageUrl;
            LocalImagePathTextBox.Text = Game.LocalImagePath; // 폴더 경로 표시
            FolderPathTextBox.Text = Game.FolderPath;
            ExecutableFilesTextBox.Text = string.Join("\n", Game.ExecutableFiles ?? new List<string>());
            AdditionalMetadataTextBox.Text = Game.AdditionalMetadata;

            // 1. 현재 화면에 표시될 큰 이미지 초기화 (DB에 저장된 대표 이미지 사용)
            DisplayedFullImagePath = Game.CoverImagePath; // CoverImagePath가 절대 경로라고 가정

            // 2. 미리보기 이미지 로드
            PreviewImageFilePaths.Clear();
            if (!string.IsNullOrWhiteSpace(Game.LocalImagePath) && Directory.Exists(Game.LocalImagePath)) {
                var imageExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                try {
                    var files = Directory.EnumerateFiles(Game.LocalImagePath)
                                         .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
                    foreach (var file in files) {
                        PreviewImageFilePaths.Add(file); // 파일의 "절대 경로" 추가
                    }
                } catch (Exception ex) {
                    // 폴더 접근 오류 등 처리
                    MessageBox.Show($"미리보기 이미지 로드 중 오류 발생: {Game.LocalImagePath}\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            // UI에서 GameInfo 객체로 데이터 업데이트
            Game.Title = TitleTextBox.Text;
            Game.Creator = CreatorTextBox.Text;
            Game.Genres = GenresTextBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(g => g.Trim()).ToList();
            Game.Rating = RatingTextBox.Text;
            Game.CoverImageUrl = CoverImageUrlTextBox.Text;
            // Game.LocalImagePath, Game.CoverImagePath 등은 여기서 변경하지 않음
            Game.AdditionalMetadata = AdditionalMetadataTextBox.Text;

            DialogResult = true;
            Close();
        }

        private void RatingUpButton_Click(object sender, RoutedEventArgs e) {
            if (double.TryParse(RatingTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentValue)) {
                double newValue = currentValue + RatingStep;
                if (newValue > MaxRating) newValue = MaxRating;
                newValue = Math.Round(newValue, 2);
                RatingTextBox.Text = newValue.ToString("N2", CultureInfo.InvariantCulture);
            }
        }

        private void RatingDownButton_Click(object sender, RoutedEventArgs e) {
            if (double.TryParse(RatingTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentValue)) {
                double newValue = currentValue - RatingStep;
                if (newValue < MinRating) newValue = MinRating;
                newValue = Math.Round(newValue, 2);
                RatingTextBox.Text = newValue.ToString("N2", CultureInfo.InvariantCulture);
            }
        }

        // 미리보기 이미지 클릭 이벤트 핸들러
        private void PreviewImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (sender is FrameworkElement frameworkElement && frameworkElement.DataContext is string imagePath) {
                // imagePath는 클릭된 이미지의 "절대 경로"임
                DisplayedFullImagePath = imagePath;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}