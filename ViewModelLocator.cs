using DLGameViewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DLGameViewer
{
    /// <summary>
    /// XAML에서 ViewModel에 접근하기 위한 로케이터 클래스입니다.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// MainViewModel에 접근하기 위한 속성입니다.
        /// </summary>
        public MainViewModel MainViewModel => App.ServiceProvider.GetRequiredService<MainViewModel>();
    }
} 