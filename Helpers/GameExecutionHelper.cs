using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
// using System.Windows.Controls; // 직접 UI를 생성하지 않으므로 주석 처리
using DLGameViewer.Models;
using DLGameViewer.Dialogs; // 새로 추가된 Dialog 네임스페이스

namespace DLGameViewer.Helpers {
    public static class GameExecutionHelper {
        public static void RunGame(GameInfo game, Window owner) // owner 파라미터는 Dialog를 위해 유지
        {
            if (game == null) { // GameInfo null 체크 추가
                MessageBox.Show("게임 정보를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Directory.Exists(game.FolderPath)) {
                MessageBox.Show($"게임 폴더를 찾을 수 없습니다: {game.FolderPath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (game.ExecutableFiles == null || !game.ExecutableFiles.Any()) {
                MessageBox.Show("실행 가능한 파일이 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (game.ExecutableFiles.Count == 1) {
                RunExecutable(game.ExecutableFiles.First());
            } else {
                ShowExecutableSelectionDialog(game.ExecutableFiles, owner);
            }
        }

        private static void ShowExecutableSelectionDialog(List<string> executableFiles, Window owner) {
            var selectionDialog = new ExecutableSelectionDialog(executableFiles) {
                Owner = owner // 부모 윈도우 설정
            };

            if (selectionDialog.ShowDialog() == true) {
                if (!string.IsNullOrEmpty(selectionDialog.SelectedExecutablePath)) {
                    RunExecutable(selectionDialog.SelectedExecutablePath);
                }
            }
        }

        private static void RunExecutable(string executablePath) {
            try {
                string extension = Path.GetExtension(executablePath).ToLower();

                if (extension == ".exe") {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = executablePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(executablePath)
                    });
                } else if (extension == ".html") {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = executablePath,
                        UseShellExecute = true
                    });
                } else {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = executablePath,
                        UseShellExecute = true
                    });
                }
            } catch (Exception ex) {
                MessageBox.Show($"파일 실행 중 오류가 발생했습니다: {ex.Message}", "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}