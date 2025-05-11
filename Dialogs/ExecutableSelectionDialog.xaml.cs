using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace DLGameViewer.Dialogs {
    public partial class ExecutableSelectionDialog : Window {
        private readonly List<string> _executableFullPaths;
        public string? SelectedExecutablePath { get; private set; }

        public ExecutableSelectionDialog(List<string> executableFullPaths) {
            InitializeComponent();
            Owner = Application.Current.MainWindow; // 부모 윈도우 설정
            _executableFullPaths = executableFullPaths;
            ExecutableListBox.ItemsSource = executableFullPaths.Select(Path.GetFileName).ToList();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            if (ExecutableListBox.SelectedIndex >= 0) {
                SelectedExecutablePath = _executableFullPaths[ExecutableListBox.SelectedIndex];
                DialogResult = true;
                Close();
            } else {
                MessageBox.Show("실행할 파일을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecutableListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            // ListBox의 아이템 위에서 더블클릭했는지 확인
            // (ListBox 자체의 빈 공간 더블클릭은 무시하기 위함이지만, ItemTemplate을 사용하면 보통 아이템 위에서 발생)
            if (ExecutableListBox.SelectedItem != null) {
                // OkButton_Click 로직과 동일하게 처리
                OkButton_Click(sender, e);
            }
        }
    }
}