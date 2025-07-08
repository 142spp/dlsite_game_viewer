using DLGameViewer.Helpers;

namespace DLGameViewer.Models
{
    public class AppSettings
    {
        public int PageSize { get; set; } = 50;
        public ThemeType CurrentTheme { get; set; } = ThemeType.Dark;
    }
}
