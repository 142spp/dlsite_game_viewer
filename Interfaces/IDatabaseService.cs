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
        /// 데이터베이스를 초기화합니다.
        /// </summary>
        Task InitializeDatabaseAsync();

        /// <summary>
        /// 게임 정보를 데이터베이스에 추가하고, 추가된 항목의 ID를 반환합니다.
        /// </summary>
        /// <param name="game">게임 정보</param>
        /// <returns>추가된 게임의 ID</returns>
        Task<long> AddGameAsync(GameInfo game);

        /// <summary>
        /// 모든 게임 정보를 가져옵니다. (페이지네이션 없이)
        /// </summary>
        /// <returns>게임 정보 목록</returns>
        Task<List<GameInfo>> GetAllGamesAsync();

        /// <summary>
        /// RJ 코드로 게임 정보를 가져옵니다.
        /// </summary>
        /// <param name="rjCode">게임 RJ 코드</param>
        /// <returns>게임 정보</returns>
        Task<GameInfo?> GetGameAsync(string rjCode);

        /// <summary>
        /// RJ 코드로 게임 정보가 이미 존재하는지 확인합니다.
        /// </summary>
        /// <param name="rjCode">게임 RJ 코드</param>
        /// <returns>존재 여부</returns>
        Task<bool> GameExistsAsync(string rjCode);

        /// <summary>
        /// 게임 정보를 업데이트합니다.
        /// </summary>
        /// <param name="game">업데이트할 게임 정보</param>
        Task UpdateGameAsync(GameInfo game);

        /// <summary>
        /// RJ 코드로 게임 정보를 삭제합니다.
        /// </summary>
        /// <param name="rjCode">삭제할 게임 RJ 코드</param>
        Task DeleteGameAsync(string rjCode);
        
        /// <summary>
        /// 커버 이미지를 저장하고 해당 경로를 DB에 업데이트합니다.
        /// </summary>
        /// <param name="rjCode">게임 RJ 코드</param>
        /// <param name="imagePath">저장된 이미지의 로컬 경로</param>
        Task SaveCoverImageAsync(string rjCode, string imagePath);

        /// <summary>
        /// RJ 코드로 커버 이미지 경로를 가져옵니다.
        /// </summary>
        /// <param name="rjCode">게임 RJ 코드</param>
        /// <returns>커버 이미지의 로컬 경로 또는 null</returns>
        Task<string?> GetCoverImageAsync(string rjCode);

        /// <summary>
        /// 지정된 페이지, 정렬 기준, 검색 조건에 따라 게임 목록을 반환합니다.
        /// </summary>
        /// <param name="pageNumber">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지당 게임 수</param>
        /// <param name="sortBy">정렬 기준 필드 이름</param>
        /// <param name="isAscending">오름차순 정렬 여부</param>
        /// <param name="searchTerm">검색어 (선택 사항)</param>
        /// <param name="searchField">검색 대상 필드 (선택 사항, 예: "Title", "CircleName", "All")</param>
        /// <returns>요청된 페이지의 게임 정보 목록</returns>
        Task<List<GameInfo>> GetGamesAsync(int pageNumber, int pageSize, string sortBy, bool isAscending, string? searchTerm = null, string? searchField = null);

        /// <summary>
        /// 검색 조건에 따른 전체 게임 수를 반환합니다.
        /// </summary>
        /// <param name="searchTerm">검색어 (선택 사항)</param>
        /// <param name="searchField">검색 대상 필드 (선택 사항)</param>
        /// <returns>전체 게임 수</returns>
        Task<int> GetTotalGameCountAsync(string? searchTerm = null, string? searchField = null);

        /// <summary>
        /// 데이터베이스를 초기화(테이블 재생성)합니다.
        /// </summary>
        Task ResetDatabaseAsync();

        /// <summary>
        /// 모든 게임 데이터를 삭제합니다.
        /// </summary>
        /// <returns>삭제된 행의 수</returns>
        Task<int> ClearAllGamesAsync();
    }
} 