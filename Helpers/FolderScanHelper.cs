using DLGameViewer.Models;
using DLGameViewer.Services; // DatabaseService와 DatabaseError를 위해 추가
using DLGameViewer.Interfaces; // 인터페이스 사용을 위해 추가
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading; // SemaphoreSlim 사용을 위해 추가

namespace DLGameViewer.Helpers {
    public static class ScanProcessHelper {
        // 진행 상황 업데이트를 위한 대리자 정의
        public delegate void ProgressUpdateCallback(string message, GameInfo? gameInfo, long gameId);

        public static async Task ProcessFolderScanAsync(
            string folderPath,
            List<GameInfo> scannedGamesOutput,
            IFolderScannerService folderScannerService,
            IWebMetadataService webMetadataService,
            IDatabaseService databaseService,
            ProgressUpdateCallback progressCallback
        ) {
            // 스캔 시작 메시지
            string scanMessage = $"'{folderPath}' 폴더를 스캔 중입니다...";
            progressCallback(scanMessage, null, -1);
            
            // 폴더 스캔 시작
            var localScannedGames = await folderScannerService.ScanDirectoryAsync(folderPath);
            // scannedGamesOutput은 이 메서드 내에서 직접 사용되지 않고, 호출 측에서 결과를 받기 위함이므로
            // localScannedGames를 기반으로 작업 후, 최종적으로 업데이트된 게임 정보를 scannedGamesOutput에 반영할 수 있습니다.
            // 여기서는 일단 localScannedGames를 직접 사용합니다.

            if (localScannedGames.Any()) {
                string scanResultMessage = $"{localScannedGames.Count}개의 게임 폴더 발견, 정보 처리 중...";
                progressCallback(scanResultMessage, null, -1);

                var tasks = new List<Task>();
                // 동시 작업 수를 5개로 제한 (DLsite 요청 부하 고려)
                var semaphore = new SemaphoreSlim(5); 

                foreach (var game in localScannedGames) {
                    tasks.Add(Task.Run(async () => {
                        await semaphore.WaitAsync();
                        try {
                            string initialStatusMessage = $"게임 폴더 정보 처리 시작: {Path.GetFileName(game.FolderPath)}, 식별자: {game.Identifier}";
                            progressCallback(initialStatusMessage, game, -1);

                            if (databaseService.IsGameExists(game.Identifier)) {
                                string duplicateMessage = $"  -> {game.Identifier}: 이미 데이터베이스에 존재합니다. 건너뜀.";
                                progressCallback(duplicateMessage, game, -1);
                                return;
                            }

                            GameInfo fetchedGameInfo = await webMetadataService.FetchMetadataAsync(game.Identifier);

                            if (fetchedGameInfo != null && fetchedGameInfo.Identifier == game.Identifier) {
                                fetchedGameInfo.FolderPath = game.FolderPath;
                                fetchedGameInfo.ExecutableFiles = game.ExecutableFiles;
                                fetchedGameInfo.SaveFolderPath = game.SaveFolderPath;
                                fetchedGameInfo.DateAdded = DateTime.Now;

                                long addResult = databaseService.AddGame(fetchedGameInfo);
                                if (addResult > 0) {
                                    string successMessage = $"  -> '{fetchedGameInfo.Title}' ({game.Identifier}): 웹 메타데이터와 함께 데이터베이스에 추가됨 (ID: {addResult}).";
                                    progressCallback(successMessage, fetchedGameInfo, addResult);
                                    // 성공적으로 추가된 게임을 scannedGamesOutput에 추가 (동기화 문제 고려 필요 시 Lock 사용)
                                    lock (scannedGamesOutput) {
                                       scannedGamesOutput.Add(fetchedGameInfo);
                                    }
                                } else {
                                    string errorMessage = $"  -> {game.Identifier}: 데이터베이스 추가 중 오류 발생 (코드: {addResult}).";
                                    progressCallback(errorMessage, null, -1);
                                }
                            } else {
                                string noMetadataMessage = $"  -> {game.Identifier}: 웹에서 메타데이터를 가져오지 못했습니다. 로컬 정보({game.Title})만 사용합니다.";
                                progressCallback(noMetadataMessage, game, -1);
                                
                                game.DateAdded = DateTime.Now; // 로컬 정보에도 추가 날짜 설정
                                long addResult = databaseService.AddGame(game);
                                if (addResult > 0) {
                                    string localSuccessMessage = $"  -> '{game.Title}' ({game.Identifier}): 로컬 정보만 데이터베이스에 추가됨 (ID: {addResult}).";
                                    progressCallback(localSuccessMessage, game, addResult);
                                    lock (scannedGamesOutput) {
                                        scannedGamesOutput.Add(game);
                                    }
                                } else {
                                    string localErrorMessage = $"  -> {game.Identifier}: 데이터베이스 추가 중 오류 발생 (코드: {addResult}).";
                                    progressCallback(localErrorMessage, null, -1);
                                }
                            }
                        } catch (Exception ex) {
                            // 개별 작업 실패 시 로깅 및 콜백
                            string taskErrorMessage = $"  -> {game.Identifier}: 처리 중 예외 발생: {ex.Message}";
                            progressCallback(taskErrorMessage, game, -1);
                        } finally {
                            semaphore.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);

            } else {
                string noGamesMessage = "지정된 폴더에서 인식 가능한 게임 정보를 찾지 못했습니다.";
                progressCallback(noGamesMessage, null, -1);
            }
            // 완료 메시지
            progressCallback("스캔 완료", null, -1);
        }
    }
} 