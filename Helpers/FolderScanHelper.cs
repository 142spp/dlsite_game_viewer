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
        // 진행 상황 업데이트를 위한 대리자 정의
        public delegate void ProgressUpdateCallback(string message, GameInfo? gameInfo, long gameId);

        public static async Task ProcessFolderScanAsync(
            string folderPath,
            List<GameInfo> scannedGamesOutput,
            FolderScannerService folderScannerService,
            WebMetadataService webMetadataService,
            DatabaseService databaseService,
            ProgressUpdateCallback progressCallback
        ) {
            // 스캔 시작 메시지
            string scanMessage = $"'{folderPath}' 폴더를 스캔 중입니다...";
            progressCallback(scanMessage, null, -1);
            
            // 폴더 스캔 시작
            var localScannedGames = await folderScannerService.ScanDirectoryAsync(folderPath);
            scannedGamesOutput.AddRange(localScannedGames);

            if (localScannedGames.Any()) {
                // 폴더 스캔 결과 콜백 호출
                string scanResultMessage = $"{localScannedGames.Count}개의 게임 폴더 발견, 정보 처리 중...";
                progressCallback(scanResultMessage, null, -1);

                foreach (var game in localScannedGames) {
                    // 폴더 발견 시 중간 업데이트
                    string statusMessage = $"게임 폴더 발견: {Path.GetFileName(game.FolderPath)}, 식별자: {game.Identifier}, 실행파일 수: {game.ExecutableFiles.Count}";
                    progressCallback(statusMessage, game, -1);

                    // 중복 식별자 확인 - IsGameExists 함수 사용
                    if (databaseService.IsGameExists(game.Identifier)) {
                        string duplicateMessage = $"  -> {game.Identifier}: 이미 데이터베이스에 존재합니다. 건너뜀.";
                        progressCallback(duplicateMessage, game, -1);
                        continue; // 이미 존재하면 다음 게임으로 넘어감
                    }

                    // 웹 메타데이터 가져오기
                    GameInfo? fetchedGameInfo = await webMetadataService.FetchMetadataAsync(game.Identifier);
                    long addResult;

                    if (fetchedGameInfo != null) {
                        fetchedGameInfo.FolderPath = game.FolderPath;
                        fetchedGameInfo.ExecutableFiles = game.ExecutableFiles;

                        addResult = databaseService.AddGame(fetchedGameInfo);
                        if (addResult >= 0) { // 성공 (ID 반환)
                            string successMessage = $"  -> '{fetchedGameInfo.Title}' ({game.Identifier}): 웹 메타데이터와 함께 데이터베이스에 추가됨 (ID: {addResult}).";
                            progressCallback(successMessage, fetchedGameInfo, addResult);
                        } else { 
                            string errorMessage = $"  -> {game.Identifier}: 데이터베이스 추가 중 오류 발생 (코드: {addResult}).";
                            progressCallback(errorMessage, null, -1);
                        }
                    } else {
                        string noMetadataMessage = $"  -> {game.Identifier}: 웹에서 메타데이터를 가져오지 못했습니다. 로컬 정보({game.Title})만 사용합니다.";
                        progressCallback(noMetadataMessage, game, -1);
                        
                        addResult = databaseService.AddGame(game);
                        if (addResult >= 0) { // 성공 (ID 반환)
                            string localSuccessMessage = $"  -> '{game.Title}' ({game.Identifier}): 로컬 정보만 데이터베이스에 추가됨 (ID: {addResult}).";
                            progressCallback(localSuccessMessage, game, addResult);
                        } else {
                            string localErrorMessage = $"  -> {game.Identifier}: 데이터베이스 추가 중 오류 발생 (코드: {addResult}).";
                            progressCallback(localErrorMessage, null, -1);
                        }
                    }
                }
            } else {
                string noGamesMessage = "지정된 폴더에서 인식 가능한 게임 정보를 찾지 못했습니다.";
                progressCallback(noGamesMessage, null, -1);
            }
            // 완료 메시지
            progressCallback("스캔 완료", null, -1);
        }
    }
} 