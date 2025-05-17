using System;
using System.Text;
using System.Windows.Input;

namespace DLGameViewer.ViewModels
{
    public class ScanResultViewModel : ViewModelBase
    {
        private string _scanResultText = string.Empty;

        public string ScanResultText
        {
            get => _scanResultText;
            set => SetProperty(ref _scanResultText, value);
        }

        public ICommand CloseCommand { get; }

        public event EventHandler RequestClose;

        public ScanResultViewModel()
        {
            CloseCommand = CreateCommand(param => ExecuteClose());
        }

        private void ExecuteClose()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 스캔 결과에 새 텍스트를 추가합니다
        /// </summary>
        public void AppendText(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            ScanResultText += message + Environment.NewLine;
        }

        /// <summary>
        /// 스캔 결과 텍스트를 설정합니다
        /// </summary>
        public void SetText(string message)
        {
            ScanResultText = message ?? string.Empty;
        }

        /// <summary>
        /// 스캔 결과를 초기화합니다
        /// </summary>
        public void Clear()
        {
            ScanResultText = string.Empty;
        }
    }
} 