#nullable disable

using System;
using System.ComponentModel;
using System.Windows;
using DLGameViewer.ViewModels;

namespace DLGameViewer.Dialogs
{
    /// <summary>
    /// ScanResultDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ScanResultDialog : Window
    {
        private readonly ScanResultViewModel _viewModel;
        
        public ScanResultDialog()
        {
            InitializeComponent();
            
            // ViewModel 생성 및 이벤트 연결
            _viewModel = new ScanResultViewModel();
            _viewModel.RequestClose += ViewModel_RequestClose;
            
            // 데이터 컨텍스트 설정
            DataContext = _viewModel;
            
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
        
        private void ViewModel_RequestClose(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 스캔 결과에 새 텍스트를 추가합니다
        /// </summary>
        public void AppendText(string message)
        {
            // UI 스레드에서 실행 (Dispatcher 사용)
            Dispatcher.Invoke(() => {
                _viewModel.AppendText(message);
            });
        }

        /// <summary>
        /// 스캔 결과 텍스트를 설정합니다
        /// </summary>
        public void SetText(string message)
        {
            Dispatcher.Invoke(() => {
                _viewModel.SetText(message);
            });
        }

        /// <summary>
        /// 스캔 결과를 초기화합니다
        /// </summary>
        public void Clear()
        {
            Dispatcher.Invoke(() => {
                _viewModel.Clear();
            });
        }
    }
} 