using Microsoft.Data.Sqlite;
using DLGameViewer.Models; // GameInfo 모델을 사용하기 위해 추가
using DLGameViewer.Interfaces; // 인터페이스를 사용하기 위해 추가
using System;
using System.IO;
using System.Collections.Generic; // List<GameInfo>를 위해 추가
using System.Text.Json; // JSON 직렬화를 위해 추가
using System.Diagnostics; // Stopwatch를 사용하기 위해 추가
using System.Linq; // ToArray() 등 사용을 위해 추가
using System.Threading.Tasks;
using System.Text; // For StringBuilder
// using Newtonsoft.Json; // JsonConvert를 사용하지 않으므로 주석 처리 또는 삭제

enum DatabaseError {
    Success = 0,
    DuplicateIdentifier = -1,
    DatabaseError = -2,
    UnknownError = -3,
}

namespace DLGameViewer.Services {
    public class DatabaseService : IDatabaseService {
        private readonly string _databasePath;

        public DatabaseService() {
            // 애플리케이션 실행 파일이 있는 디렉토리를 기준으로 Data 폴더 경로 설정
            //string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            //string projectRootPath = Path.GetFullPath(Path.Combine(executablePath, "../../..")); // 일반적인 .NET 프로젝트 구조에서 프로젝트 루트 추정
            //string dataFolderPath = Path.Combine(projectRootPath, "Data");

            // 현재 작업 디렉토리 (프로젝트 루트)를 기준으로 Data 폴더 경로 설정
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // WPF 앱에서는 실행 파일이 bin/Debug/netX.X-windows 에 위치하므로, 프로젝트 루트로 이동
            // 이 경로는 빌드 구성(Debug/Release) 및 대상 프레임워크에 따라 달라질 수 있어 주의 필요
            // 좀 더 견고한 방법은 설정 파일에서 경로를 읽거나, 사용자 정의 가능한 경로를 사용하는 것
            // 여기서는 프로젝트 개발 중 단순화를 위해 상대 경로를 사용
            string projectRootPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..")); 
            if (!Directory.Exists(Path.Combine(projectRootPath, "Data")) && Directory.Exists(baseDir)) { // 개발 중 VSCode 실행 경로 대응
                projectRootPath = baseDir; // 터미널에서 직접 dotnet run 하는 경우 BaseDirectory가 프로젝트 루트일 수 있음
            }
            
            string dataFolderPath = Path.Combine(projectRootPath, "Data");
            Directory.CreateDirectory(dataFolderPath); // Data 폴더가 없으면 생성
            _databasePath = Path.Combine(dataFolderPath, "dlgameviewer.sqlite");

            // 생성자에서 InitializeDatabaseAsync()를 직접 호출하는 대신,
            // 애플리케이션 시작 시점에서 별도로 호출하도록 변경합니다.
            // Task.Run(() => InitializeDatabaseAsync()).Wait(); // 또는 다른 동기화 방식 사용 가능하나, UI 스레드 블로킹 주의
        }

        public async Task InitializeDatabaseAsync() {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                    CREATE TABLE IF NOT EXISTS GameInfo (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Identifier TEXT UNIQUE NOT NULL,
                        Title TEXT NOT NULL,
                        Creator TEXT,
                        GameType TEXT,
                        Genres TEXT, -- Comma-separated values
                        SalesCount INTEGER,
                        Rating TEXT,
                        RatingCount INTEGER,
                        CoverImageUrl TEXT,
                        CoverImagePath TEXT,
                        LocalImagePath TEXT,
                        FolderPath TEXT UNIQUE NOT NULL,
                        ExecutableFiles TEXT, -- Comma-separated values
                        SaveFolderPath TEXT,
                        DateAdded TEXT,
                        LastPlayed TEXT,
                        ReleaseDate TEXT,
                        FileSize TEXT,
                        PlayTime TEXT,
                        UserMemo TEXT,
                        IsArchive INTEGER DEFAULT 0, -- Boolean as INTEGER
                        ArchiveFilePath TEXT
                    );
                ";
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task ResetDatabaseAsync() {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                using (var transaction = (SqliteTransaction)await connection.BeginTransactionAsync()) {
                    try {
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        
                        command.CommandText = "DROP TABLE IF EXISTS GameInfo;";
                        await command.ExecuteNonQueryAsync();
                        
                        command.CommandText = 
                        @"
                            CREATE TABLE IF NOT EXISTS GameInfo (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Identifier TEXT UNIQUE NOT NULL,
                                Title TEXT NOT NULL,
                                Creator TEXT,
                                GameType TEXT,
                                Genres TEXT,
                                SalesCount INTEGER,
                                Rating TEXT,
                                RatingCount INTEGER,
                                CoverImageUrl TEXT,
                                CoverImagePath TEXT,
                                LocalImagePath TEXT,
                                FolderPath TEXT UNIQUE NOT NULL,
                                SaveFolderPath TEXT,
                                ExecutableFiles TEXT,
                                DateAdded TEXT,
                                LastPlayed TEXT,
                                ReleaseDate TEXT,
                                FileSize TEXT,
                                PlayTime TEXT,
                                UserMemo TEXT,
                                IsArchive INTEGER DEFAULT 0, -- Boolean as INTEGER
                                ArchiveFilePath TEXT
                            );
                        ";
                        await command.ExecuteNonQueryAsync();
                        
                        await transaction.CommitAsync();
                    }
                    catch (Exception) {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
        
        public async Task<int> ClearAllGamesAsync() {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM GameInfo;";
                return await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AddGameAsync(GameInfo game) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO GameInfo (Identifier, Title, Creator, GameType, Genres, SalesCount, Rating, RatingCount, 
                                        CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, SaveFolderPath, ExecutableFiles, 
                                        DateAdded, LastPlayed, ReleaseDate, FileSize, PlayTime, 
                                        UserMemo, IsArchive, ArchiveFilePath)
                    VALUES ($Identifier, $Title, $Creator, $GameType, $Genres, $SalesCount, $Rating, $RatingCount, 
                            $CoverImageUrl, $CoverImagePath, $LocalImagePath, $FolderPath, $SaveFolderPath, $ExecutableFiles, 
                            $DateAdded, $LastPlayed, $ReleaseDate, $FileSize, $PlayTime, 
                            $UserMemo, $IsArchive, $ArchiveFilePath);
                ";

                command.Parameters.AddWithValue("$Identifier", game.Identifier);
                command.Parameters.AddWithValue("$Title", game.Title);
                command.Parameters.AddWithValue("$Creator", game.Creator ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$GameType", game.GameType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$Genres", game.Genres != null && game.Genres.Any() ? JsonSerializer.Serialize(game.Genres) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$SalesCount", game.SalesCount == 0 ? (object)DBNull.Value : game.SalesCount);
                command.Parameters.AddWithValue("$Rating", game.Rating ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$RatingCount", game.RatingCount == 0 ? (object)DBNull.Value : game.RatingCount);
                command.Parameters.AddWithValue("$CoverImageUrl", game.CoverImageUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImagePath", game.CoverImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$LocalImagePath", game.LocalImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$FolderPath", game.FolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$SaveFolderPath", game.SaveFolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$ExecutableFiles", game.ExecutableFiles != null && game.ExecutableFiles.Any() ? JsonSerializer.Serialize(game.ExecutableFiles) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$DateAdded", game.DateAdded == DateTime.MinValue ? (object)DBNull.Value : game.DateAdded.ToString("o"));
                command.Parameters.AddWithValue("$LastPlayed", game.LastPlayed.HasValue ? game.LastPlayed.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$ReleaseDate", game.ReleaseDate.HasValue ? game.ReleaseDate.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$FileSize", game.FileSize ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$PlayTime", game.PlayTime == TimeSpan.Zero ? (object)DBNull.Value : game.PlayTime.ToString());
                command.Parameters.AddWithValue("$UserMemo", game.UserMemo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$IsArchive", game.IsArchive ? 1 : 0);
                command.Parameters.AddWithValue("$ArchiveFilePath", game.ArchiveFilePath ?? (object)DBNull.Value);

                try {
                    await command.ExecuteNonQueryAsync();
                } catch (SqliteException ex) {
                    // Log or handle specific SQLite errors, e.g., unique constraint violation
                    Console.WriteLine($"DatabaseService.AddGameAsync Error: {ex.Message}");
                    // Consider re-throwing or returning an error code/status
                    throw; 
                }
            }
        }

        public async Task<List<GameInfo>> GetAllGamesAsync() {
            var games = new List<GameInfo>();
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM GameInfo;";
                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        games.Add(CreateGameInfoFromReader(reader));
                    }
                }
            }
            return games;
        }

        public async Task<GameInfo?> GetGameAsync(string rjCode) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM GameInfo WHERE Identifier = $Identifier;";
                command.Parameters.AddWithValue("$Identifier", rjCode);
                using (var reader = await command.ExecuteReaderAsync()) {
                    if (await reader.ReadAsync()) {
                        return CreateGameInfoFromReader(reader);
                    }
                }
            }
            return null;
        }
        
        public async Task<bool> GameExistsAsync(string rjCode) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(1) FROM GameInfo WHERE Identifier = $Identifier;";
                command.Parameters.AddWithValue("$Identifier", rjCode);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }

        public async Task UpdateGameAsync(GameInfo game) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = 
                @"UPDATE GameInfo SET 
                    Title = $Title, 
                    Creator = $Creator, 
                    GameType = $GameType, 
                    Genres = $Genres, 
                    SalesCount = $SalesCount, 
                    Rating = $Rating, 
                    RatingCount = $RatingCount, 
                    CoverImageUrl = $CoverImageUrl, 
                    CoverImagePath = $CoverImagePath,
                    LocalImagePath = $LocalImagePath, 
                    FolderPath = $FolderPath, 
                    SaveFolderPath = $SaveFolderPath,
                    ExecutableFiles = $ExecutableFiles, 
                    DateAdded = $DateAdded, 
                    LastPlayed = $LastPlayed, 
                    ReleaseDate = $ReleaseDate, 
                    FileSize = $FileSize, 
                    PlayTime = $PlayTime,
                    UserMemo = $UserMemo,
                    IsArchive = $IsArchive,
                    ArchiveFilePath = $ArchiveFilePath
                  WHERE Identifier = $Identifier;";

                command.Parameters.AddWithValue("$Identifier", game.Identifier);
                command.Parameters.AddWithValue("$Title", game.Title);
                command.Parameters.AddWithValue("$Creator", game.Creator ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$GameType", game.GameType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$Genres", game.Genres != null && game.Genres.Any() ? JsonSerializer.Serialize(game.Genres) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$SalesCount", game.SalesCount == 0 ? (object)DBNull.Value : game.SalesCount);
                command.Parameters.AddWithValue("$Rating", game.Rating ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$RatingCount", game.RatingCount == 0 ? (object)DBNull.Value : game.RatingCount);
                command.Parameters.AddWithValue("$CoverImageUrl", game.CoverImageUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImagePath", game.CoverImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$LocalImagePath", game.LocalImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$FolderPath", game.FolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$SaveFolderPath", game.SaveFolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$ExecutableFiles", game.ExecutableFiles != null && game.ExecutableFiles.Any() ? JsonSerializer.Serialize(game.ExecutableFiles) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$DateAdded", game.DateAdded == DateTime.MinValue ? (object)DBNull.Value : game.DateAdded.ToString("o"));
                command.Parameters.AddWithValue("$LastPlayed", game.LastPlayed.HasValue ? game.LastPlayed.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$ReleaseDate", game.ReleaseDate.HasValue ? game.ReleaseDate.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$FileSize", game.FileSize ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$PlayTime", game.PlayTime == TimeSpan.Zero ? (object)DBNull.Value : game.PlayTime.ToString());
                command.Parameters.AddWithValue("$UserMemo", game.UserMemo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$IsArchive", game.IsArchive ? 1 : 0);
                command.Parameters.AddWithValue("$ArchiveFilePath", game.ArchiveFilePath ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteGameAsync(string rjCode) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM GameInfo WHERE Identifier = $Identifier;";
                command.Parameters.AddWithValue("$Identifier", rjCode);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task SaveCoverImageAsync(string rjCode, string imagePath) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE GameInfo SET LocalImagePath = $LocalImagePath WHERE Identifier = $Identifier;";
                command.Parameters.AddWithValue("$LocalImagePath", imagePath);
                command.Parameters.AddWithValue("$Identifier", rjCode);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<string?> GetCoverImageAsync(string rjCode) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT LocalImagePath FROM GameInfo WHERE Identifier = $Identifier;";
                command.Parameters.AddWithValue("$Identifier", rjCode);
                var result = await command.ExecuteScalarAsync();
                return result is DBNull ? null : (string?)result;
            }
        }

        public async Task<List<GameInfo>> GetGamesAsync(int pageNumber, int pageSize, string sortBy, bool isAscending, string? searchTerm = null, string? searchField = null) {
            var games = new List<GameInfo>();
            var sqlBuilder = new StringBuilder("SELECT * FROM GameInfo");
            var parameters = new Dictionary<string, object?>();

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchTerm) && !string.IsNullOrWhiteSpace(searchField)) {
                if (searchField.Equals("All", StringComparison.OrdinalIgnoreCase)) {
                    var searchableFields = new[] { "Title", "Creator", "Identifier", "Genres", "GameType" };
                    var searchConditions = searchableFields.Select(field => $"{field} LIKE $SearchTerm");
                    sqlBuilder.Append($" WHERE ({string.Join(" OR ", searchConditions)})");
                } else {
                    sqlBuilder.Append($" WHERE {SanitizeIdentifier(searchField)} LIKE $SearchTerm");
                }
                parameters["$SearchTerm"] = $"%{searchTerm}%";
            }

            // 특수 정렬 케이스 처리
            bool needsCustomSort = false;
            bool isIdentifierSort = false;
            bool isFileSizeSort = false;

            if (!string.IsNullOrWhiteSpace(sortBy)) {
                if (sortBy.Equals("Identifier", StringComparison.OrdinalIgnoreCase) || 
                    sortBy.Equals("FileSize", StringComparison.OrdinalIgnoreCase)) {
                    // 기본 쿼리에서는 일반 정렬 사용, 후처리에서 식별자 숫자, 바이트 기준으로 정렬
                    needsCustomSort = true;
                    isIdentifierSort = true;
                } else {} 
                // 일반 정렬
                string direction = isAscending ? "ASC" : "DESC";
                sqlBuilder.Append($" ORDER BY {SanitizeIdentifier(sortBy)} {direction}");
            } else {
                // 기본 정렬
                sqlBuilder.Append(" ORDER BY Title DESC");
            }

            // 페이지네이션은 커스텀 정렬 후에 적용하기 위해 여기서는 적용하지 않음
            if (!needsCustomSort) {
                sqlBuilder.Append(" LIMIT $PageSize OFFSET $Offset;");
                parameters["$PageSize"] = pageSize;
                parameters["$Offset"] = (pageNumber - 1) * pageSize;
            }

            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = sqlBuilder.ToString();
                foreach (var param in parameters) {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        games.Add(CreateGameInfoFromReader(reader));
                    }
                }
            }

            // 커스텀 정렬 적용
            if (needsCustomSort) {
                if (isIdentifierSort) {
                    games = SortGamesByIdentifier(games, isAscending);
                } else if (isFileSizeSort) {
                    games = SortGamesByFileSize(games, isAscending);
                }

                // 정렬 후 페이지네이션 적용
                int totalCount = games.Count;
                games = games.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            }

            return games;
        }
        
        // 식별자 기준 정렬 (RJ/VJ 뒤의 숫자 기준)
        private List<GameInfo> SortGamesByIdentifier(List<GameInfo> games, bool isAscending) {
            return games.OrderBy(g => {
                if (string.IsNullOrEmpty(g.Identifier))
                    return isAscending ? int.MaxValue : int.MinValue;

                // RJ, VJ 등의 접두사 추출
                string prefix = string.Empty;
                string numericPart = g.Identifier;

                if (g.Identifier.Length >= 2) {
                    prefix = g.Identifier.Substring(0, 2);
                    numericPart = g.Identifier.Substring(2);
                }

                // 숫자 부분만 추출하여 정수로 변환
                if (int.TryParse(numericPart, out int numericValue)) {
                    return numericValue;
                }
                
                return isAscending ? int.MaxValue : int.MinValue;
            }, isAscending ? Comparer<int>.Default : Comparer<int>.Create((x, y) => y.CompareTo(x))).ToList();
        }

        // 파일 크기 기준 정렬 (KB, MB, GB 단위 고려)
        private List<GameInfo> SortGamesByFileSize(List<GameInfo> games, bool isAscending) {
            return games.OrderBy(g => {
                if (string.IsNullOrEmpty(g.FileSize))
                    return isAscending ? long.MinValue : long.MaxValue;

                // 파일 크기를 바이트 단위로 변환
                return ConvertFileSizeToBytes(g.FileSize);
            }, isAscending ? Comparer<long>.Default : Comparer<long>.Create((x, y) => y.CompareTo(x))).ToList();
        }

        // 파일 크기 문자열(예: "1.5 MB")을 바이트로 변환
        private long ConvertFileSizeToBytes(string? fileSizeStr) {
            if (string.IsNullOrEmpty(fileSizeStr))
                return 0;

            try {
                // 숫자와 단위 분리
                string[] parts = fileSizeStr.Split(' ');
                if (parts.Length != 2)
                    return 0;

                if (!double.TryParse(parts[0], out double size))
                    return 0;

                string unit = parts[1].ToUpperInvariant();
                
                return unit switch {
                    "B" => (long)size,
                    "KB" => (long)(size * 1024),
                    "MB" => (long)(size * 1024 * 1024),
                    "GB" => (long)(size * 1024 * 1024 * 1024),
                    "TB" => (long)(size * 1024 * 1024 * 1024 * 1024),
                    _ => 0
                };
            }
            catch {
                return 0;
            }
        }

        public async Task<int> GetTotalGameCountAsync(string? searchTerm = null, string? searchField = null) {
            var sqlBuilder = new StringBuilder("SELECT COUNT(*) FROM GameInfo");
            var parameters = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(searchTerm) && !string.IsNullOrWhiteSpace(searchField)) {
                 if (searchField.Equals("All", StringComparison.OrdinalIgnoreCase)) {
                    var searchableFields = new[] { "Title", "Creator", "Identifier", "Genres", "GameType" };
                    var searchConditions = searchableFields.Select(field => $"{field} LIKE $SearchTerm");
                    sqlBuilder.Append($" WHERE ({string.Join(" OR ", searchConditions)})");
                } else {
                    sqlBuilder.Append($" WHERE {SanitizeIdentifier(searchField)} LIKE $SearchTerm");
                }
                parameters["$SearchTerm"] = $"%{searchTerm}%";
            }

            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = sqlBuilder.ToString();
                foreach (var param in parameters) {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
        
        // Helper method to prevent SQL injection in identifiers like column names.
        // THIS IS A CRITICAL SECURITY MEASURE.
        private string SanitizeIdentifier(string identifier)
        {
            // Allow alphanumeric characters and underscore.
            // This is a basic sanitizer. For production, consider a allow-list of known valid column names.
            var sanitized = new StringBuilder();
            foreach (char c in identifier)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sanitized.Append(c);
                }
            }
            if (sanitized.Length == 0) {
                // Fallback to a default or throw an exception if identifier is completely invalid
                // For sorting, it's safer to fall back to a default known column like 'Id' or 'Title'
                // or simply not sort if the column is not recognized.
                // Here, we'll throw an argument exception as an invalid column name is a programming error.
                throw new ArgumentException($"Invalid identifier for SQL: {identifier}");
            }
            return sanitized.ToString();
        }

        // Helper method to create GameInfo object from SqliteDataReader
        private GameInfo CreateGameInfoFromReader(SqliteDataReader reader) {
            // Nullable 값 처리를 위해 GetString 대신 GetNullableString 같은 확장 메서드를 만들거나, 
            // reader.IsDBNull을 사용하여 각 컬럼을 확인하는 것이 더 안전합니다.
            // 아래는 간결성을 위해 기본적인 GetString 등을 사용하며, 실제 사용 시에는 Null 처리 강화 필요.
            
            return new GameInfo {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                Identifier = reader.GetString(reader.GetOrdinal("Identifier")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Creator = reader.IsDBNull(reader.GetOrdinal("Creator")) ? null : reader.GetString(reader.GetOrdinal("Creator")),
                GameType = reader.IsDBNull(reader.GetOrdinal("GameType")) ? null : reader.GetString(reader.GetOrdinal("GameType")),
                Genres = reader.IsDBNull(reader.GetOrdinal("Genres")) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("Genres"))) ?? new List<string>(),
                SalesCount = reader.IsDBNull(reader.GetOrdinal("SalesCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("SalesCount")),
                Rating = reader.IsDBNull(reader.GetOrdinal("Rating")) ? null : reader.GetString(reader.GetOrdinal("Rating")),
                RatingCount = reader.IsDBNull(reader.GetOrdinal("RatingCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("RatingCount")),
                CoverImageUrl = reader.IsDBNull(reader.GetOrdinal("CoverImageUrl")) ? null : reader.GetString(reader.GetOrdinal("CoverImageUrl")),
                CoverImagePath = reader.IsDBNull(reader.GetOrdinal("CoverImagePath")) ? null : reader.GetString(reader.GetOrdinal("CoverImagePath")),
                LocalImagePath = reader.IsDBNull(reader.GetOrdinal("LocalImagePath")) ? null : reader.GetString(reader.GetOrdinal("LocalImagePath")),
                FolderPath = reader.IsDBNull(reader.GetOrdinal("FolderPath")) ? string.Empty : reader.GetString(reader.GetOrdinal("FolderPath")),
                ExecutableFiles = reader.IsDBNull(reader.GetOrdinal("ExecutableFiles")) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("ExecutableFiles"))) ?? new List<string>(),
                SaveFolderPath = reader.IsDBNull(reader.GetOrdinal("SaveFolderPath")) ? null : reader.GetString(reader.GetOrdinal("SaveFolderPath")),
                DateAdded = reader.IsDBNull(reader.GetOrdinal("DateAdded")) ? DateTime.MinValue : DateTime.Parse(reader.GetString(reader.GetOrdinal("DateAdded"))),
                LastPlayed = reader.IsDBNull(reader.GetOrdinal("LastPlayed")) ? (DateTime?)null : DateTime.Parse(reader.GetString(reader.GetOrdinal("LastPlayed"))),
                ReleaseDate = reader.IsDBNull(reader.GetOrdinal("ReleaseDate")) ? (DateTime?)null : DateTime.Parse(reader.GetString(reader.GetOrdinal("ReleaseDate"))),
                FileSize = reader.IsDBNull(reader.GetOrdinal("FileSize")) ? null : reader.GetString(reader.GetOrdinal("FileSize")),
                PlayTime = reader.IsDBNull(reader.GetOrdinal("PlayTime")) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(reader.GetOrdinal("PlayTime"))),
                UserMemo = reader.IsDBNull(reader.GetOrdinal("UserMemo")) ? null : reader.GetString(reader.GetOrdinal("UserMemo")),
                IsArchive = reader.IsDBNull(reader.GetOrdinal("IsArchive")) ? false : reader.GetInt32(reader.GetOrdinal("IsArchive")) == 1,
                ArchiveFilePath = reader.IsDBNull(reader.GetOrdinal("ArchiveFilePath")) ? null : reader.GetString(reader.GetOrdinal("ArchiveFilePath"))
            };
        }
        
        // DeserializeList, ParseIdentifierString, ParseFileSizeString 헬퍼 메서드는 
        // CreateGameInfoFromReader 내부에서 직접 처리하거나, 필요하다면 유지합니다.
        // 여기서는 CreateGameInfoFromReader에서 직접 처리하는 방식으로 변경했습니다.

    }
} 