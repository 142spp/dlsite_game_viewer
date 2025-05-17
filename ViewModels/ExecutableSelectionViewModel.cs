using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace DLGameViewer.ViewModels {
    public class ExecutableSelectionViewModel : ViewModelBase {
        private readonly List<string> _executableFullPaths;
        private string _selectedExecutablePath;
        private int _selectedIndex = -1;
        private List<string> _executableNames;

        public List<string> ExecutableNames {
            get => _executableNames;
            private set => SetProperty(ref _executableNames, value);
        }

        public int SelectedIndex {
            get => _selectedIndex;
            set {
                if (SetProperty(ref _selectedIndex, value) && value >= 0 && value < _executableFullPaths.Count) {
                    SelectedExecutablePath = _executableFullPaths[value];
                }
            }
        }

        public string SelectedExecutablePath {
            get => _selectedExecutablePath;
            private set => SetProperty(ref _selectedExecutablePath, value);
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectExecutableCommand { get; }

        public event EventHandler<bool> RequestClose;

        public ExecutableSelectionViewModel(List<string> executableFullPaths) {
            _executableFullPaths = executableFullPaths ?? new List<string>();

            // 실행 파일 이름 목록 생성 (경로 없이 파일명만)
            ExecutableNames = _executableFullPaths.Select(Path.GetFileName).ToList();

            // 명령 초기화
            OkCommand = CreateCommand(param => ExecuteOk());
            CancelCommand = CreateCommand(param => ExecuteCancel());
            SelectExecutableCommand = CreateCommand(param => ExecuteSelect());
        }

        private void ExecuteOk() {
            // 선택된 항목이 있으면 다이얼로그를 닫습니다
            if (SelectedIndex >= 0) {
                RequestClose?.Invoke(this, true);
            }
            else if (_executableFullPaths.Count == 1) {
                // 실행 파일이 하나뿐인 경우 자동 선택
                SelectedIndex = 0;
                RequestClose?.Invoke(this, true);
            }
        }

        private void ExecuteCancel() {
            RequestClose?.Invoke(this, false);
        }

        private void ExecuteSelect() {
            ExecuteOk();
        }
    }
}