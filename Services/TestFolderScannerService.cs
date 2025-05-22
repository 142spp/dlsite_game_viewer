using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLGameViewer.Interfaces;
using DLGameViewer.Models;

namespace DLGameViewer.Services {
    public class TestFolderScannerService : IFolderScannerService {
        private readonly Random _random = new Random();
        private readonly string[] _creators = { "AliceSoft", "Eushully", "Lilith", "Softhouse-Seal", "Tinkerbell" };
        private readonly string[] _genres = { "RPG", "ADV", "SLG", "ACT", "RTS", "SIM" };
        private readonly string[] _gameTypes = { "RPG", "ADV", "SLG", "ACT", "RTS", "SIM" };

        public async Task<List<GameInfo>> ScanDirectoryAsync(string folderPath) {
            var games = new List<GameInfo>();
            var listFilePath = Path.Combine(folderPath, "RJGameList.txt");

            if (!File.Exists(listFilePath)) {
                throw new FileNotFoundException("RJGameList.txt 파일을 찾을 수 없습니다.", listFilePath);
            }

            var lines = await File.ReadAllLinesAsync(listFilePath);
            foreach (var line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = System.Text.RegularExpressions.Regex.Match(line, @"(RJ|VJ)\d{6,8}");
                if (!match.Success) continue;
                
                var identifier = match.Value;
                var title = line.Substring(match.Index + match.Length).Trim();

                var game = new GameInfo {
                    Identifier = identifier,
                    Title = title,
                    Creator = _creators[_random.Next(_creators.Length)],
                    Genres = new List<string> { _genres[_random.Next(_genres.Length)] },
                    GameType = _gameTypes[_random.Next(_gameTypes.Length)],
                    Rating = _random.Next(1, 6).ToString(),
                    FileSize = $"{_random.Next(100, 10000)} MB",
                    DateAdded = DateTime.Now.AddDays(-_random.Next(0, 365)),
                    LastPlayed = _random.Next(2) == 1 ? DateTime.Now.AddDays(-_random.Next(0, 30)) : null,
                    ReleaseDate = DateTime.Now.AddDays(-_random.Next(0, 1000)),
                    PlayTime = TimeSpan.FromMinutes(_random.Next(0, 1000)),
                    FolderPath = Path.Combine(folderPath, identifier)
                };
                games.Add(game);
            }

            // 실제 폴더도 생성
            foreach (var game in games) {
                Directory.CreateDirectory(game.FolderPath);
            }

            return null;
        }
    }
} 