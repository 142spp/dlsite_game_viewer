using DLGameViewer.Models;
using System;
using System.Windows;
using DLGameViewer.ViewModels;

namespace DLGameViewer.Dialogs
{
    /// <summary>
    /// GameInfoDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GameInfoDialog : Window
    {
        private readonly GameInfoViewModel _viewModel;
        
        // 게임 정보를 외부에서 접근할 수 있도록 제공
        public GameInfo Game => _viewModel.GetUpdatedGame();
        public string SearchCategory { get; private set; }
        public string SearchTerm { get; private set; }

        public GameInfoDialog(GameInfo gameInfo)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            
            // ViewModel 생성 및 이벤트 연결
            _viewModel = new GameInfoViewModel(gameInfo);
            _viewModel.RequestClose += ViewModel_RequestClose;
            _viewModel.SearchRequested += ViewModel_SearchRequested;
            
            // 데이터 컨텍스트 설정
            DataContext = _viewModel;
            
            // 윈도우 크기 변경 이벤트 연결
            SizeChanged += GameInfoDialog_SizeChanged;
        }

        private void ViewModel_SearchRequested(string category, string term)
        {
            SearchCategory = category;
            SearchTerm = term;
            DialogResult = true; // Or a custom result if you want to differentiate
            Close();
        }

        // 윈도우 크기 변경 시 이미지 다시 로드 (성능 최적화를 위해 필요한 경우에만 새로고침)
        private void GameInfoDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 작은 크기 변경은 무시 (성능 최적화)
            if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 100 || 
                Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 100)
            {
                // 현재 표시 중인 이미지 경로를 임시 저장
                string currentPath = _viewModel.DisplayedFullImagePath;
                
                // 이미지 새로고침 (null로 설정했다가 다시 원래 경로로 설정)
                if (!string.IsNullOrEmpty(currentPath))
                {
                    _viewModel.DisplayedFullImagePath = null;
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        _viewModel.DisplayedFullImagePath = currentPath;
                    }));
                }
            }
        }

        private void ViewModel_RequestClose(object sender, bool dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }
    }
}