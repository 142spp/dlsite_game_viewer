using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization; // JsonIgnore 사용을 위해 추가

namespace DLGameViewer.Models
{
    public class GameInfo : INotifyPropertyChanged
    {
        private long _id;
        private string _identifier = string.Empty;
        private string _title = string.Empty;
        private string _creator = string.Empty;
        private string _gameType = string.Empty;
        private List<string> _genres = new List<string>();
        private int _salesCount;
        private string _rating = string.Empty;
        private int _ratingCount;
        private string _coverImageUrl = string.Empty;
        private string _coverImagePath = string.Empty;
        private string _localImagePath = string.Empty;
        private string _folderPath = string.Empty;
        private string _saveFolderPath = string.Empty;
        private List<string> _executableFiles = new List<string>();
        private DateTime _dateAdded;
        private DateTime? _lastPlayed;
        private DateTime? _releaseDate;
        private string _fileSize = string.Empty;
        private TimeSpan _playTime;
        private string _userMemo = string.Empty;

        // 정렬 및 필터링 최적화를 위한 추가 속성
        private long _identifierAsLong;
        private long _fileSizeInBytes;

        public long Id { get => _id; set => SetProperty(ref _id, value); }
        public string Identifier { get => _identifier; set => SetProperty(ref _identifier, value); }
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Creator { get => _creator; set => SetProperty(ref _creator, value); }
        public string GameType { get => _gameType; set => SetProperty(ref _gameType, value); }
        public List<string> Genres { get => _genres; set => SetProperty(ref _genres, value); }
        public int SalesCount { get => _salesCount; set => SetProperty(ref _salesCount, value); }
        public string Rating { get => _rating; set => SetProperty(ref _rating, value); }
        public int RatingCount { get => _ratingCount; set => SetProperty(ref _ratingCount, value); }
        public string CoverImageUrl { get => _coverImageUrl; set => SetProperty(ref _coverImageUrl, value); }
        public string CoverImagePath { get => _coverImagePath; set => SetProperty(ref _coverImagePath, value); }
        public string LocalImagePath { get => _localImagePath; set => SetProperty(ref _localImagePath, value); }
        public string FolderPath { get => _folderPath; set => SetProperty(ref _folderPath, value); }
        public string SaveFolderPath { get => _saveFolderPath; set => SetProperty(ref _saveFolderPath, value); }
        public List<string> ExecutableFiles { get => _executableFiles; set => SetProperty(ref _executableFiles, value); }
        public DateTime DateAdded { get => _dateAdded; set => SetProperty(ref _dateAdded, value); }
        public DateTime? LastPlayed { get => _lastPlayed; set => SetProperty(ref _lastPlayed, value); }
        public DateTime? ReleaseDate { get => _releaseDate; set => SetProperty(ref _releaseDate, value); }
        public string FileSize { get => _fileSize; set => SetProperty(ref _fileSize, value); }
        public TimeSpan PlayTime { get => _playTime; set => SetProperty(ref _playTime, value); }
        public string UserMemo { get => _userMemo; set => SetProperty(ref _userMemo, value); }

        // 정렬 및 필터링 최적화 속성 (데이터베이스에는 직접 저장하지 않음)
        [JsonIgnore] // JSON 직렬화 시 제외 (DB 저장용이 아님)
        public long IdentifierAsLong { get => _identifierAsLong; set => SetProperty(ref _identifierAsLong, value); }
        [JsonIgnore] // JSON 직렬화 시 제외
        public long FileSizeInBytes { get => _fileSizeInBytes; set => SetProperty(ref _fileSizeInBytes, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public GameInfo()
        {
            // 기본 생성자에서 Identifier, Title 등을 "" 로 초기화하여 null 참조 방지
            Identifier = string.Empty;
            Title = string.Empty;
            Creator = string.Empty;
            GameType = string.Empty;
            Rating = string.Empty;
            FileSize = string.Empty;
            CoverImageUrl = string.Empty;
            CoverImagePath = string.Empty;
            LocalImagePath = string.Empty;
            FolderPath = string.Empty;
            UserMemo = string.Empty;
        }
    }
} 