using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLGameViewer.Models;
using DLGameViewer.Interfaces;

namespace DLGameViewer.Services
{
    public class FolderScannerService : IFolderScannerService
    {
        // RJ, VJ로 시작하고 그 뒤에 2자리 이상의 숫자가 오는 패턴 (예: RJ123456, VJ00)
        private static readonly Regex IdentifierRegex = new Regex(@"(RJ|VJ)[\s_]?\d{5,}", RegexOptions.IgnoreCase);
        private readonly IDatabaseService _databaseService;
        private readonly IWebMetadataService _webMetadataService;

        // 생성자 추가: DatabaseService와 WebMetadataService를 주입받음
        public FolderScannerService(IDatabaseService databaseService, IWebMetadataService webMetadataService){
            _databaseService = databaseService;
            _webMetadataService = webMetadataService;
        }

        public async Task<List<GameInfo>> ScanDirectoryAsync(string directoryPath){
            var foundGames = new List<GameInfo>();
            if (!Directory.Exists(directoryPath)){
                return foundGames;
            }
            // 재귀적으로 폴더 스캔 시작
            await ScanFoldersRecursivelyAsync(directoryPath, foundGames);
            return foundGames;
        }

        // 재귀적으로 폴더를 스캔하는 메서드
        private async Task ScanFoldersRecursivelyAsync(string currentPath, List<GameInfo> foundGames) {
            if (!Directory.Exists(currentPath)) {
                return;
            }

            string folderName = Path.GetFileName(currentPath);
            Match match = IdentifierRegex.Match(folderName);

            // 현재 폴더가 게임 폴더인지 확인 (식별자 패턴을 포함하는지)
            if (match.Success) {
                string identifier = match.Value.Replace("_", "").Replace(" ", "").ToUpper();
                // 임시 게임 정보 생성
                var gameInfo = new GameInfo {
                    Identifier = identifier,
                    Title = folderName,
                    FolderPath = currentPath
                };
                // 실행 파일 검색 (최적화된 방식)
                await FindExecutableFilesAsync(currentPath, gameInfo);
                // 세이브 폴더 검색
                await GetSaveFolderPathAsync(currentPath, gameInfo);
                // 게임 정보를 찾았으면 리스트에 추가
                foundGames.Add(gameInfo);
                // 식별자를 찾았으므로 이 폴더의 하위 폴더는 더 이상 조사하지 않음
                return;
            }

            // 하위 폴더들을 가져와서 재귀적으로 각 폴더 탐색
            try {
                var subDirectories = await Task.Run(() => Directory.GetDirectories(currentPath));
                foreach (var subDir in subDirectories) {
                    await ScanFoldersRecursivelyAsync(subDir, foundGames);
                }
            } catch (UnauthorizedAccessException) {
                // 접근 권한이 없는 폴더는 무시
            } catch (Exception ex) {
                // 기타 예외 처리 (로깅 등 필요시 추가)
                Console.WriteLine($"폴더 스캔 중 오류 발생: {ex.Message}");
            }
        }

        // 실행 파일을 검색하는 최적화된 메서드
        private async Task FindExecutableFilesAsync(string folderPath, GameInfo gameInfo) {
            // 1단계: 최상위 폴더에서만 실행 파일 검색
            var topLevelExecutables = await Task.Run(() => 
                Directory.GetFiles(folderPath, "*.exe", SearchOption.TopDirectoryOnly).ToList()
            );
            // 최상위 폴더에서 실행 파일을 찾았으면 그것만 사용
            if (topLevelExecutables.Any()) {
                gameInfo.ExecutableFiles.AddRange(topLevelExecutables);
                return;
            }
            // 2단계: 최상위에서 찾지 못했다면 모든 하위 폴더 검색
            var allExecutables = await Task.Run(() => 
                Directory.GetFiles(folderPath, "*.exe", SearchOption.AllDirectories).ToList()
            );
            
            gameInfo.ExecutableFiles.AddRange(allExecutables);
        }

        private async Task GetSaveFolderPathAsync(string folderPath, GameInfo gameInfo) {
            // 쯔꾸르 기본 세이브 폴더 경로
            var defaultSaveFolderPath = Path.Combine(folderPath, "www\\save");
            if (Directory.Exists(defaultSaveFolderPath)) {
                gameInfo.SaveFolderPath = defaultSaveFolderPath;
                return;
            }
            // 쯔꾸르 기본 세이브 폴더 경로가 없다면 게임 폴더 경로에서 세이브 폴더 경로 찾기
            var saveFolderPath = await Task.Run(() =>
                Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories)
                    .Where(dir => Path.GetFileName(dir).Equals("save", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault()
            );
            gameInfo.SaveFolderPath = saveFolderPath ?? string.Empty;
        }
    }
} 