using DLGameViewer.Models;
using DLGameViewer.Services; // DatabaseService와 DatabaseError를 위해 추가
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLGameViewer.Helpers {
    public static class ScanProcessHelper {
        public static async Task<StringBuilder> ProcessFolderScanAsync(
            string folderPath,
            List<GameInfo> scannedGamesOutput,
            FolderScannerService folderScannerService,
            WebMetadataService webMetadataService,
            DatabaseService databaseService
        ) {
            StringBuilder sb = new StringBuilder();
            var localScannedGames = await folderScannerService.ScanDirectoryAsync(folderPath);
            scannedGamesOutput.AddRange(localScannedGames);

            if (localScannedGames.Any()) {
                sb.AppendLine($"[{localScannedGames.Count}개의 게임 정보를 폴더에서 찾았습니다:]");
                foreach (var game in localScannedGames) {
                    sb.AppendLine($"- 식별자: {game.Identifier}, 폴더명: {Path.GetFileName(game.FolderPath)}, 실행파일 수: {game.ExecutableFiles.Count}");

                    GameInfo? fetchedGameInfo = await webMetadataService.FetchMetadataAsync(game.Identifier);
                    long addResult;

                    if (fetchedGameInfo != null) {
                        fetchedGameInfo.FolderPath = game.FolderPath;
                        fetchedGameInfo.ExecutableFiles = game.ExecutableFiles;
                        
                        addResult = databaseService.AddGame(fetchedGameInfo);
                        if (addResult >= 0) { // 성공 (ID 반환)
                            sb.AppendLine($"  -> '{fetchedGameInfo.Title}' ({game.Identifier}): 웹 메타데이터와 함께 데이터베이스에 추가됨 (ID: {addResult}).");
                        } else if (addResult == (long)DatabaseError.DuplicateIdentifier) { 
                            sb.AppendLine($"  -> 중복 식별자 {game.Identifier}. 데이터베이스 추가 건너뜀.");
                        } else { 
                            sb.AppendLine($"  -> {game.Identifier}: 데이터베이스 추가 중 오류 발생 (코드: {addResult}).");
                        }
                    } else {
                        sb.AppendLine($"  -> {game.Identifier}: 웹에서 메타데이터를 가져오지 못했습니다. 로컬 정보({game.Title})만 사용합니다.");
                        addResult = databaseService.AddGame(game);
                        if (addResult >= 0) { // 성공 (ID 반환)
                            sb.AppendLine($"  -> '{game.Title}' ({game.Identifier}): 로컬 정보만 데이터베이스에 추가됨 (ID: {addResult}).");
                        } else if (addResult == (long)DatabaseError.DuplicateIdentifier) {
                            sb.AppendLine($"  -> 중복 식별자 {game.Identifier}. 데이터베이스 추가 건너뜀.");
                        } else {
                            sb.AppendLine($"  -> {game.Identifier}: 데이터베이스 추가 중 오류 발생 (코드: {addResult}).");
                        }
                    }
                }
            } else {
                sb.AppendLine("지정된 폴더에서 인식 가능한 게임 정보를 찾지 못했습니다.");
            }
            return sb;
        }
    }
} 