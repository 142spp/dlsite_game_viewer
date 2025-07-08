using DLGameViewer.Models;

namespace DLGameViewer.Interfaces
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        void LoadSettings();
        void SaveSettings();
    }
}
