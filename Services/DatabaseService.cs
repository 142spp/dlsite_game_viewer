using Microsoft.Data.Sqlite;
using DLGameViewer.Models; // GameInfo 모델을 사용하기 위해 추가
using DLGameViewer.Interfaces; // 인터페이스를 사용하기 위해 추가
using System;
using System.IO;
using System.Collections.Generic; // List<GameInfo>를 위해 추가
using System.Text.Json; // JSON 직렬화를 위해 추가
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

            InitializeDatabase();
        }

        private void InitializeDatabase() {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                // GameInfo 테이블 생성 SQL (테이블이 이미 존재하지 않을 경우에만 생성)
                // Id: 기본 키, 자동 증가
                // Identifier: 고유 식별자, 중복 불가
                // Genres, ExecutableFiles는 별도 테이블로 관리하거나 JSON 문자열로 저장할 수 있습니다.
                // 여기서는 간단하게 TEXT로 저장하고, 필요시 파싱하는 형태로 갑니다.
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
                        UserMemo TEXT
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        // 데이터베이스 초기화(재설정) 메서드
        // 이 메서드는 기존 데이터를 모두 삭제하고 테이블을 재생성합니다.
        public void ResetDatabase() {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();
                
                // 트랜잭션 시작
                using (var transaction = connection.BeginTransaction()) {
                    try {
                        var command = connection.CreateCommand();
                        
                        // 기존 테이블 삭제
                        command.CommandText = "DROP TABLE IF EXISTS GameInfo;";
                        command.ExecuteNonQuery();
                        
                        // 테이블 재생성
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
                                UserMemo TEXT
                            );
                        ";
                        command.ExecuteNonQuery();
                        
                        // 트랜잭션 커밋
                        transaction.Commit();
                    }
                    catch (Exception) {
                        // 오류 발생 시 롤백
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        // 데이터베이스에서 모든 게임 데이터만 삭제하고 테이블 구조는 유지하는 메서드
        public int ClearAllGames() {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();
                
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM GameInfo;";
                
                return command.ExecuteNonQuery(); // 삭제된 행의 수 반환
            }
        }

        // 여기에 CRUD 및 기타 데이터베이스 작업 메서드들이 추가될 예정입니다.
        // 예: public void AddGame(GameInfo game) { ... }
        // 예: public List<GameInfo> GetAllGames() { ... }
        // 예: public GameInfo GetGameByIdentifier(string identifier) { ... }

        public long AddGame(GameInfo game) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO GameInfo (Identifier, Title, Creator, GameType, Genres, SalesCount, Rating, RatingCount, 
                                        CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, SaveFolderPath, ExecutableFiles, 
                                        DateAdded, LastPlayed, ReleaseDate, FileSize, PlayTime, 
                                        UserMemo)
                    VALUES ($Identifier, $Title, $Creator, $GameType, $Genres, $SalesCount, $Rating, $RatingCount, 
                            $CoverImageUrl, $CoverImagePath, $LocalImagePath, $FolderPath, $SaveFolderPath, $ExecutableFiles, 
                            $DateAdded, $LastPlayed, $ReleaseDate, $FileSize, $PlayTime, 
                            $UserMemo);
                    
                    SELECT last_insert_rowid(); // 삽입된 행의 ID를 반환
                ";

                command.Parameters.AddWithValue("$Identifier", game.Identifier);
                command.Parameters.AddWithValue("$Title", game.Title);
                command.Parameters.AddWithValue("$Creator", game.Creator ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$GameType", game.GameType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$Genres", game.Genres.Any() ? JsonSerializer.Serialize(game.Genres) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$SalesCount", game.SalesCount == 0 ? (object)DBNull.Value : game.SalesCount);
                command.Parameters.AddWithValue("$Rating", game.Rating ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$RatingCount", game.RatingCount == 0 ? (object)DBNull.Value : game.RatingCount);
                command.Parameters.AddWithValue("$CoverImageUrl", game.CoverImageUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImagePath", game.CoverImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$LocalImagePath", game.LocalImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$FolderPath", game.FolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$SaveFolderPath", game.SaveFolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$ExecutableFiles", game.ExecutableFiles.Any() ? JsonSerializer.Serialize(game.ExecutableFiles) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$DateAdded", game.DateAdded == DateTime.MinValue ? (object)DBNull.Value : game.DateAdded.ToString("o"));
                command.Parameters.AddWithValue("$LastPlayed", game.LastPlayed.HasValue ? game.LastPlayed.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$ReleaseDate", game.ReleaseDate.HasValue ? game.ReleaseDate.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$FileSize", game.FileSize ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$PlayTime", game.PlayTime == TimeSpan.Zero ? (object)DBNull.Value : game.PlayTime.ToString());

                command.Parameters.AddWithValue("$UserMemo", game.UserMemo ?? (object)DBNull.Value);

                // ExecuteScalar는 INSERT 후 last_insert_rowid() 값을 반환
                try {
                    long newId = Convert.ToInt64(command.ExecuteScalar());
                    return newId;
                }
                catch (SqliteException ex) {
                    if (ex.SqliteErrorCode == 19) {
                        return (long) DatabaseError.DuplicateIdentifier;
                    } else {
                        return (long) DatabaseError.DatabaseError;
                    }
                }
            }
        }

        public List<GameInfo> GetAllGames() {
            var games = new List<GameInfo>();

            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT Id, Identifier, Title, Creator, GameType, Genres, SalesCount, Rating, RatingCount, 
                           CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, SaveFolderPath, ExecutableFiles,  
                           DateAdded, LastPlayed, ReleaseDate, FileSize, PlayTime, 
                           UserMemo
                    FROM GameInfo;
                ";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var game = new GameInfo {
                            Id = reader.GetInt64(0),
                            Identifier = reader.GetString(1),
                            Title = reader.GetString(2),
                            Creator = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            GameType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Genres = DeserializeList(reader, 5),
                            SalesCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                            Rating = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            RatingCount = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                            CoverImageUrl = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            CoverImagePath = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            LocalImagePath = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                            FolderPath = reader.GetString(12),
                            SaveFolderPath = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                            ExecutableFiles = DeserializeList(reader, 14),
                            DateAdded = reader.IsDBNull(15) ? DateTime.MinValue : DateTime.Parse(reader.GetString(15)),
                            LastPlayed = reader.IsDBNull(16) ? (DateTime?)null : DateTime.Parse(reader.GetString(16)),
                            ReleaseDate = reader.IsDBNull(17) ? (DateTime?)null : DateTime.Parse(reader.GetString(17)),
                            FileSize = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                            PlayTime = reader.IsDBNull(19) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(19)),
                            UserMemo = reader.IsDBNull(20) ? string.Empty : reader.GetString(20)
                        };
                        games.Add(game);
                    }
                }
            }
            return games;
        }

        public GameInfo GetGameById(long id) {
            GameInfo game = null;

            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT Id, Identifier, Title, Creator, GameType, Genres, SalesCount, Rating, RatingCount, 
                           CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, SaveFolderPath, ExecutableFiles,  
                           DateAdded, LastPlayed, ReleaseDate, FileSize, PlayTime, 
                           UserMemo
                    FROM GameInfo
                    WHERE Id = $Id;
                ";
                command.Parameters.AddWithValue("$Id", id);

                using (var reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        game = new GameInfo {
                            Id = reader.GetInt64(0),
                            Identifier = reader.GetString(1),
                            Title = reader.GetString(2),
                            Creator = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            GameType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Genres = DeserializeList(reader, 5),
                            SalesCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                            Rating = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            RatingCount = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                            CoverImageUrl = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            CoverImagePath = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            LocalImagePath = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                            FolderPath = reader.GetString(12),
                            SaveFolderPath = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                            ExecutableFiles = DeserializeList(reader, 14),
                            DateAdded = reader.IsDBNull(15) ? DateTime.MinValue : DateTime.Parse(reader.GetString(15)),
                            LastPlayed = reader.IsDBNull(16) ? (DateTime?)null : DateTime.Parse(reader.GetString(16)),
                            ReleaseDate = reader.IsDBNull(17) ? (DateTime?)null : DateTime.Parse(reader.GetString(17)),
                            FileSize = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                            PlayTime = reader.IsDBNull(19) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(19)),
                            UserMemo = reader.IsDBNull(20) ? string.Empty : reader.GetString(20)
                        };
                    }
                }
            }
            return game;
        }

        // JSON 문자열에서 문자열 리스트로 역직렬화하는 헬퍼 메서드
        private List<string> DeserializeList(SqliteDataReader reader, int columnIndex) {
            if (reader.IsDBNull(columnIndex))
                return new List<string>();

            string json = reader.GetString(columnIndex);
            try {
                // 호환성 유지: 이전 형식(쉼표로 구분된 문자열)인지 확인
                if (!json.StartsWith("[") && json.Contains(",")) {
                    return json.Split(',').ToList();
                }
                
                // JSON 형식이면 역직렬화
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch {
                // 역직렬화 실패 시 빈 리스트 반환
                return new List<string>();
            }
        }

        public bool IsGameExists(string identifier) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT COUNT(*) FROM GameInfo WHERE Identifier = $Identifier;
                ";
                command.Parameters.AddWithValue("$Identifier", identifier);

                try {
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                } catch (SqliteException) {
                    return false;
                }
            }
        }

        public int UpdateGame(GameInfo game) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    UPDATE GameInfo SET 
                        Identifier = $Identifier, 
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
                        ExecutableFiles = $ExecutableFiles, 
                        DateAdded = $DateAdded, 
                        LastPlayed = $LastPlayed, 
                        ReleaseDate = $ReleaseDate, 
                        FileSize = $FileSize, 
                        PlayTime = $PlayTime, 
                        UserMemo = $UserMemo
                    WHERE Id = $Id;
                ";

                command.Parameters.AddWithValue("$Id", game.Id);
                command.Parameters.AddWithValue("$Identifier", game.Identifier);
                command.Parameters.AddWithValue("$Title", game.Title);
                command.Parameters.AddWithValue("$Creator", game.Creator ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$GameType", game.GameType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$Genres", game.Genres.Any() ? JsonSerializer.Serialize(game.Genres) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$SalesCount", game.SalesCount == 0 ? (object)DBNull.Value : game.SalesCount);
                command.Parameters.AddWithValue("$Rating", game.Rating ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$RatingCount", game.RatingCount == 0 ? (object)DBNull.Value : game.RatingCount);
                command.Parameters.AddWithValue("$CoverImageUrl", game.CoverImageUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImagePath", game.CoverImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$LocalImagePath", game.LocalImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$FolderPath", game.FolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$ExecutableFiles", game.ExecutableFiles.Any() ? JsonSerializer.Serialize(game.ExecutableFiles) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$DateAdded", game.DateAdded == DateTime.MinValue ? (object)DBNull.Value : game.DateAdded.ToString("o"));
                command.Parameters.AddWithValue("$LastPlayed", game.LastPlayed.HasValue ? game.LastPlayed.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$ReleaseDate", game.ReleaseDate.HasValue ? game.ReleaseDate.Value.ToString("o") : (object)DBNull.Value);
                command.Parameters.AddWithValue("$FileSize", game.FileSize ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$PlayTime", game.PlayTime == TimeSpan.Zero ? (object)DBNull.Value : game.PlayTime.ToString());
                command.Parameters.AddWithValue("$UserMemo", game.UserMemo ?? (object)DBNull.Value);

                return command.ExecuteNonQuery(); // 영향을 받은 행의 수 반환
            }
        }

        public int DeleteGame(long id) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    DELETE FROM GameInfo
                    WHERE Id = $Id;
                ";

                command.Parameters.AddWithValue("$Id", id);

                return command.ExecuteNonQuery(); // 영향을 받은 행의 수를 반환
            }
        }

        // 검색 메서드 추가
        public List<GameInfo> SearchGames(string searchText) {
            var games = new List<GameInfo>();
            if (string.IsNullOrWhiteSpace(searchText)) {
                return GetAllGames(); // 검색어가 없으면 모든 게임 반환
            }

            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                // Title, Identifier, Creator, Genres 필드에서 검색 (LIKE 사용)
                // Genres는 JSON 문자열이므로, 실제로는 파싱 후 검색하거나, 별도 테이블/FTS 사용 고려 필요
                // 여기서는 간단히 문자열 포함 여부로 검색
                command.CommandText =
                @"
                    SELECT Id, Identifier, Title, Creator, GameType, Genres, SalesCount, Rating, RatingCount, 
                           CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, SaveFolderPath, ExecutableFiles,  
                           DateAdded, LastPlayed, ReleaseDate, FileSize, PlayTime, 
                           UserMemo
                    FROM GameInfo
                    WHERE Title LIKE $searchText OR 
                          Identifier LIKE $searchText OR 
                          Creator LIKE $searchText OR
                          GameType LIKE $searchText OR
                          Genres LIKE $searchText; -- 주의: Genres는 JSON 배열 문자열, 정확한 검색 어려움
                ";
                command.Parameters.AddWithValue("$searchText", $"%{searchText}%");

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var game = new GameInfo {
                            Id = reader.GetInt64(0),
                            Identifier = reader.GetString(1),
                            Title = reader.GetString(2),
                            Creator = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            GameType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Genres = DeserializeList(reader, 5),
                            SalesCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                            Rating = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            RatingCount = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                            CoverImageUrl = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            CoverImagePath = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            LocalImagePath = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                            FolderPath = reader.GetString(12),
                            ExecutableFiles = DeserializeList(reader, 13),
                            SaveFolderPath = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                            DateAdded = reader.IsDBNull(15) ? DateTime.MinValue : DateTime.Parse(reader.GetString(15)),
                            LastPlayed = reader.IsDBNull(16) ? (DateTime?)null : DateTime.Parse(reader.GetString(16)),
                            ReleaseDate = reader.IsDBNull(17) ? (DateTime?)null : DateTime.Parse(reader.GetString(17)),
                            FileSize = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                            PlayTime = reader.IsDBNull(19) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(19)),
                            UserMemo = reader.IsDBNull(20) ? string.Empty : reader.GetString(20)
                        };
                        games.Add(game);
                    }
                }
            }
            return games;
        }
    }
} 