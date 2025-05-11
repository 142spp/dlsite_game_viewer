using Microsoft.Data.Sqlite;
using DLGameViewer.Models; // GameInfo 모델을 사용하기 위해 추가
using System;
using System.IO;
using System.Collections.Generic; // List<GameInfo>를 위해 추가
// using Newtonsoft.Json; // JsonConvert를 사용하지 않으므로 주석 처리 또는 삭제

enum DatabaseError {
    Success,
    DuplicateIdentifier,
    DatabaseError,
    UnknownError,
}

namespace DLGameViewer.Services {
    public class DatabaseService {
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
                        Genres TEXT, -- Comma-separated values
                        Rating TEXT,
                        CoverImageUrl TEXT,
                        CoverImagePath TEXT,
                        LocalImagePath TEXT,
                        FolderPath TEXT UNIQUE NOT NULL,
                        ExecutableFiles TEXT, -- Comma-separated values
                        AdditionalMetadata TEXT
                    );
                ";
                command.ExecuteNonQuery();
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
                    INSERT INTO GameInfo (Identifier, Title, Creator, Genres, Rating, CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, ExecutableFiles, AdditionalMetadata)
                    VALUES ($Identifier, $Title, $Creator, $Genres, $Rating, $CoverImageUrl, $CoverImagePath, $LocalImagePath, $FolderPath, $ExecutableFiles, $AdditionalMetadata);
                    
                    SELECT last_insert_rowid(); // 삽입된 행의 ID를 반환
                ";

                command.Parameters.AddWithValue("$Identifier", game.Identifier);
                command.Parameters.AddWithValue("$Title", game.Title);
                command.Parameters.AddWithValue("$Creator", game.Creator ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$Genres", game.Genres.Any() ? string.Join(",", game.Genres) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$Rating", game.Rating ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImageUrl", game.CoverImageUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImagePath", game.CoverImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$LocalImagePath", game.LocalImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$FolderPath", game.FolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$ExecutableFiles", game.ExecutableFiles.Any() ? string.Join(",", game.ExecutableFiles) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$AdditionalMetadata", game.AdditionalMetadata ?? (object)DBNull.Value);

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
                    SELECT Id, Identifier, Title, Creator, Genres, Rating, 
                           CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, 
                           ExecutableFiles, AdditionalMetadata
                    FROM GameInfo;
                ";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var game = new GameInfo {
                            Id = reader.GetInt64(0),
                            Identifier = reader.GetString(1),
                            Title = reader.GetString(2),
                            Creator = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Genres = reader.IsDBNull(4) ? new List<string>() : reader.GetString(4).Split(',').ToList(),
                            Rating = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            CoverImageUrl = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            CoverImagePath = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            LocalImagePath = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            FolderPath = reader.GetString(9),
                            ExecutableFiles = reader.IsDBNull(10) ? new List<string>() : reader.GetString(10).Split(',').ToList(),
                            AdditionalMetadata = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                        };
                        games.Add(game);
                    }
                }
            }
            return games;
        }

        public GameInfo? GetGameById(long id) {
            GameInfo? game = null;

            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT Id, Identifier, Title, Creator, Genres, Rating, 
                           CoverImageUrl, CoverImagePath, LocalImagePath, FolderPath, 
                           ExecutableFiles, AdditionalMetadata
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
                            Genres = reader.IsDBNull(4) ? new List<string>() : reader.GetString(4).Split(',').ToList(),
                            Rating = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            CoverImageUrl = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            CoverImagePath = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            LocalImagePath = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            FolderPath = reader.GetString(9),
                            ExecutableFiles = reader.IsDBNull(10) ? new List<string>() : reader.GetString(10).Split(',').ToList(),
                            AdditionalMetadata = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                        };
                    }
                }
            }
            return game;
        }

        public int UpdateGame(GameInfo game) {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}")) {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    UPDATE GameInfo
                    SET Identifier = $Identifier,
                        Title = $Title,
                        Creator = $Creator,
                        Genres = $Genres,
                        Rating = $Rating,
                        CoverImageUrl = $CoverImageUrl,
                        CoverImagePath = $CoverImagePath,
                        LocalImagePath = $LocalImagePath,
                        FolderPath = $FolderPath,
                        ExecutableFiles = $ExecutableFiles,
                        AdditionalMetadata = $AdditionalMetadata
                    WHERE Id = $Id;
                ";

                command.Parameters.AddWithValue("$Identifier", game.Identifier);
                command.Parameters.AddWithValue("$Title", game.Title);
                command.Parameters.AddWithValue("$Creator", game.Creator ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$Genres", game.Genres.Any() ? string.Join(",", game.Genres) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$Rating", game.Rating ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImageUrl", game.CoverImageUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$CoverImagePath", game.CoverImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$LocalImagePath", game.LocalImagePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$FolderPath", game.FolderPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$ExecutableFiles", game.ExecutableFiles.Any() ? string.Join(",", game.ExecutableFiles) : (object)DBNull.Value);
                command.Parameters.AddWithValue("$AdditionalMetadata", game.AdditionalMetadata ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$Id", game.Id);

                return command.ExecuteNonQuery(); // 영향을 받은 행의 수를 반환
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
    }
} 