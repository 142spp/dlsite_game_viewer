using System;
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

        // 게임 타입 
        public string GameType { get; set; }

        // 장르 목록
        public List<string> Genres { get; set; } = new List<string>();

        // 판매수
        public int SalesCount { get; set; }

        // 평점 (문자열로 저장하여 다양한 형식 수용)
        public string Rating { get; set; }

        // 평가수
        public int RatingCount { get; set; }

        // 게임의 공식 발매일
        public DateTime? ReleaseDate { get; set; }

        // 게임 폴더 또는 주요 파일의 크기 (바이트 단위)
        public string FileSize { get; set; }

        // 웹에서 가져온 커버 이미지 URL
        public string CoverImageUrl { get; set; } // 대표 커버 이미지 하나만 우선 저장

        // 커버 이미지 파일 경로
        public string CoverImagePath { get; set; }

        // 로컬에 저장된 커버 이미지 파일 경로
        public string LocalImagePath { get; set; }

        // 원본 콘텐츠 폴더 경로
        public string FolderPath { get; set; }

        // 세이브 폴더 경로
        public string SaveFolderPath { get; set; }

        // 폴더 내 실행 파일 목록 (전체 경로)
        public List<string> ExecutableFiles { get; set; } = new List<string>();

        // 게임이 데이터베이스에 추가된 날짜
        public DateTime DateAdded { get; set; }

        // 게임을 마지막으로 플레이한 날짜
        public DateTime? LastPlayed { get; set; }

        // 총 플레이 시간
        public TimeSpan PlayTime { get; set; }

        // 기타 메타데이터 (예: 설명, 태그 등을 JSON 문자열 형태로 저장 가능)
        public string UserMemo { get; set; }
        
        public GameInfo()
        {
            // 기본 생성자에서 Identifier, Title 등을 "" 로 초기화하여 null 참조 방지
            Identifier = string.Empty;
            Title = string.Empty;
            Creator = string.Empty;
            GameType = string.Empty;
            Rating = string.Empty;
            FileSize = string.Empty;
            CoverImageUrl = string.Empty;
            CoverImagePath = string.Empty;
            LocalImagePath = string.Empty;
            FolderPath = string.Empty;
            UserMemo = string.Empty;
        }
    }
} 