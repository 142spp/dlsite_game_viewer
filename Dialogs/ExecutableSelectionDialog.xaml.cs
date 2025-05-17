using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using DLGameViewer.ViewModels;

namespace DLGameViewer.Dialogs {
    /// <summary>
    /// ExecutableSelectionDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ExecutableSelectionDialog : Window {
        private readonly ExecutableSelectionViewModel _viewModel;

        public string SelectedExecutablePath => _viewModel.SelectedExecutablePath;

        public ExecutableSelectionDialog(List<string> executableFullPaths) {
            InitializeComponent();
            
            // ViewModel 생성 및 이벤트 연결
            _viewModel = new ExecutableSelectionViewModel(executableFullPaths);
            _viewModel.RequestClose += ViewModel_RequestClose;
            
            // 데이터 컨텍스트 설정
            DataContext = _viewModel;
        }

        private void ViewModel_RequestClose(object sender, bool dialogResult) {
            DialogResult = dialogResult;
            Close();
        }

        private void ExecutableListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            // 더블클릭 시 ViewModel의 SelectExecutableCommand 실행
            if (_viewModel.SelectExecutableCommand.CanExecute(null)) {
                _viewModel.SelectExecutableCommand.Execute(null);
            }
        }
    }
}