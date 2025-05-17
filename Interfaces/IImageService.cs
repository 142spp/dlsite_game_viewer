using System.Threading.Tasks;

namespace DLGameViewer.Interfaces
{
    /// <summary>
    /// 이미지 다운로드 및 관리를 처리하는 서비스 인터페이스
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// 이미지를 다운로드하여 로컬에 저장합니다.
        /// </summary>
        /// <param name="imageUrl">이미지 URL</param>
        /// <param name="identifier">게임 식별자</param>
        /// <param name="fileName">파일 이름</param>
        /// <returns>저장된 이미지의 경로</returns>
        Task<string> DownloadAndSaveImageAsync(string imageUrl, string identifier, string fileName);
    }
} 