#nullable disable

using System.ComponentModel;
using System.Windows;

namespace DLGameViewer.Dialogs
{
    /// <summary>
    /// ScanResultDialog.xaml에 대한 상호 ��용 논리
    /// </summary>
    public partial class ScanResultDialog : Window
    {
        public ScanResultDialog()
        {
            InitializeComponent();
            
            // 닫기 이벤트 핸들러 등록
            Closing += ScanResultDialog_Closing;
        }

        /// <summary>
        /// 창이 닫힐 때 발생하는 이벤트 처리 (X 버튼 클릭 포함)
        /// </summary>
        private void ScanResultDialog_Closing(object sender, CancelEventArgs e)
        {
            // 소유 창(메인 윈도우)을 활성화
            if (Owner != null)
            {
                Owner.Activate();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 