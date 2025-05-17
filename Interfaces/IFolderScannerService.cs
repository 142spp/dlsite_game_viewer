using System.Collections.Generic;
using System.Threading.Tasks;
using DLGameViewer.Models;

namespace DLGameViewer.Interfaces
{
    /// <summary>
    /// 폴더를 스캔하여 게임 정보를 찾는 서비스 인터페이스
    /// </summary>
    public interface IFolderScannerService
    {
        /// <summary>
        /// 지정된 디렉토리를 스캔하여 게임 정보를 수집합니다.
        /// </summary>
        /// <param name="directoryPath">스캔할 디렉토리 경로</param>
        /// <returns>찾은 게임 정보 목록</returns>
        Task<List<GameInfo>> ScanDirectoryAsync(string directoryPath);
    }
} 