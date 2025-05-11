using System.Collections.Generic;

namespace DLGameViewer.Models
{
    public class GameInfo
    {
        // 데이터베이스의 기본 키 (Primary Key)
        public long Id { get; set; }

        // 폴더명 등에서 추출한 고유 식별자 (예: RJ123456)
        public string Identifier { get; set; }

        // 콘텐츠 제목
        public string Title { get; set; }

        // 제작자 또는 서클
        public string Creator { get; set; }

        // 장르 목록
        public List<string> Genres { get; set; } = new List<string>();

        // 평점 (문자열로 저장하여 다양한 형식 수용)
        public string Rating { get; set; }

        // 웹에서 가져온 커버 이미지 URL
        public string CoverImageUrl { get; set; } // 대표 커버 이미지 하나만 우선 저장

        // 로컬에 저장된 커버 이미지 파일 경로
        public string LocalImagePath { get; set; }

        // 원본 콘텐츠 폴더 경로
        public string FolderPath { get; set; }

        // 폴더 내 실행 파일 목록 (전체 경로)
        public List<string> ExecutableFiles { get; set; } = new List<string>();

        // 기타 메타데이터 (예: 설명, 태그 등을 JSON 문자열 형태로 저장 가능)
        public string AdditionalMetadata { get; set; }

        // 언제 데이터베이스에 추가되었는지 (선택적)
        // public DateTime DateAdded { get; set; }

        public GameInfo()
        {
            // 기본 생성자에서 Identifier, Title 등을 "" 로 초기화하여 null 참조 방지
            Identifier = string.Empty;
            Title = string.Empty;
            Creator = string.Empty;
            Rating = string.Empty;
            CoverImageUrl = string.Empty;
            LocalImagePath = string.Empty;
            FolderPath = string.Empty;
            AdditionalMetadata = string.Empty;
        }
    }
} 