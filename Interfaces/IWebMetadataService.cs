using System.Threading.Tasks;
using DLGameViewer.Models;

namespace DLGameViewer.Interfaces
{
    /// <summary>
    /// 웹에서 게임 메타데이터를 가져오는 서비스 인터페이스
    /// </summary>
    public interface IWebMetadataService
    {
        /// <summary>
        /// 식별자를 기반으로 웹에서 게임 메타데이터를 가져옵니다.
        /// </summary>
        /// <param name="identifier">게임 식별자</param>
        /// <returns>게임 정보</returns>
        Task<GameInfo> FetchMetadataAsync(string identifier);
    }
} 