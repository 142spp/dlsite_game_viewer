using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLGameViewer.Models;

namespace DLGameViewer.Services
{
    public class FolderScannerService
    {
        // RJ, VJ로 시작하고 그 뒤에 2자리 이상의 숫자가 오는 패턴 (예: RJ123456, VJ00)
        private static readonly Regex IdentifierRegex = new Regex(@"(RJ|VJ)[\s_]?\d{5,}", RegexOptions.IgnoreCase);
        private readonly DatabaseService _databaseService;
        private readonly WebMetadataService _webMetadataService;

        // 생성자 추가: DatabaseService와 WebMetadataService를 주입받음
        public FolderScannerService(DatabaseService databaseService, WebMetadataService webMetadataService){
            _databaseService = databaseService;
            _webMetadataService = webMetadataService;
        }

        public async Task<List<GameInfo>> ScanDirectoryAsync(string directoryPath){
            var foundGames = new List<GameInfo>();

            if (!Directory.Exists(directoryPath)){
                return foundGames;
            }

            // 지정된 디렉토리의 모든 하위 디렉토리를 가져옵니다.
            // GetDirectories는 Task-returning 오버로드가 없으므로, Task.Run으로 감싸서 비동기적으로 실행합니다.
            var subDirectories = await Task.Run(() => Directory.GetDirectories(directoryPath));

            foreach (var subDirPath in subDirectories){
                string folderName = Path.GetFileName(subDirPath); // 폴더 경로에서 폴더 이름만 추출
                Match match = IdentifierRegex.Match(folderName);

                if (match.Success){
                    string identifier = match.Value.Replace("_", "").Replace(" ", "").ToUpper(); // 공백, 밑줄 제거 및 대문자화
                    
                    // 임시 게임 정보 생성
                    var gameInfo = new GameInfo{
                        Identifier = identifier, 
                        Title = folderName, 
                        FolderPath = subDirPath,
                    };

                    // 실행 파일 검색
                    var executables = await Task.Run(() => 
                        Directory.GetFiles(subDirPath, "*.exe")
                                 .Concat(Directory.GetFiles(subDirPath, "*.html"))
                                 .Concat(Directory.GetFiles(subDirPath, "*.swf"))
                                 .ToList()
                    );
                    gameInfo.ExecutableFiles.AddRange(executables);
                
                    // 게임 정보를 찾았으면 리스트에 추가
                    foundGames.Add(gameInfo);
                }
            }
            return foundGames;
        }
    }
} 