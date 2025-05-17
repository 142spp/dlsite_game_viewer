using System.Collections.Generic;
using System.Threading.Tasks;
using DLGameViewer.Models;

namespace DLGameViewer.Interfaces
{
    /// <summary>
    /// 데이터베이스 작업을 처리하는 서비스 인터페이스
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// 게임 정보를 데이터베이스에 추가합니다.
        /// </summary>
        /// <param name="game">게임 정보</param>
        /// <returns>추가된 게임의 ID 또는 오류 코드</returns>
        long AddGame(GameInfo game);

        /// <summary>
        /// 모든 게임 정보를 가져옵니다.
        /// </summary>
        /// <returns>게임 정보 목록</returns>
        List<GameInfo> GetAllGames();

        /// <summary>
        /// ID로 게임 정보를 가져옵니다.
        /// </summary>
        /// <param name="id">게임 ID</param>
        /// <returns>게임 정보</returns>
        GameInfo GetGameById(long id);

        /// <summary>
        /// 게임 정보가 이미 존재하는지 확인합니다.
        /// </summary>
        /// <param name="identifier">게임 식별자</param>
        /// <returns>존재 여부</returns>
        bool IsGameExists(string identifier);

        /// <summary>
        /// 게임 정보를 업데이트합니다.
        /// </summary>
        /// <param name="game">업데이트할 게임 정보</param>
        /// <returns>업데이트된 행의 수</returns>
        int UpdateGame(GameInfo game);

        /// <summary>
        /// 게임 정보를 삭제합니다.
        /// </summary>
        /// <param name="id">삭제할 게임 ID</param>
        /// <returns>삭제된 행의 수</returns>
        int DeleteGame(long id);

        /// <summary>
        /// 모든 게임 정보를 삭제합니다.
        /// </summary>
        /// <returns>삭제된 행의 수</returns>
        int ClearAllGames();

        /// <summary>
        /// 데이터베이스를 재설정합니다.
        /// </summary>
        void ResetDatabase();
        
        /// <summary>
        /// 검색어를 사용하여 게임을 검색합니다.
        /// </summary>
        /// <param name="searchText">검색어</param>
        /// <returns>검색 결과에 해당하는 게임 정보 목록</returns>
        List<GameInfo> SearchGames(string searchText);
    }
} 