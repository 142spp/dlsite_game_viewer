# DLGameViewer (DL 게임 뷰어)

DLGameViewer는 DLsite 게임 및 콘텐츠를 로컬에서 효율적으로 관리하고 실행할 수 있는 Windows 데스크톱 애플리케이션입니다. WPF와 MVVM 패턴을 활용하여 개발되었으며, 의존성 주입을 통한 모듈식 아키텍처를 구현했습니다.

## ✨ 주요 기능

### 🔍 콘텐츠 스캔 및 관리
- **지능형 폴더 스캔**: 로컬 폴더를 재귀적으로 스캔하여 DLsite 콘텐츠를 자동 감지
- **식별자 추출**: 폴더명에서 DLsite 식별자(RJ, VJ, RE 등)를 정확하게 추출
- **실행 파일 감지**: 게임 폴더 내 실행 가능한 파일(.exe) 자동 탐지
- **파일 크기 계산**: 폴더별 정확한 파일 크기 정보 제공

### 🌐 메타데이터 자동 수집
- **웹 스크래핑**: DLsite에서 제목, 제작자, 장르, 평점 등 자동 수집
- **이미지 관리**: 커버 이미지 자동 다운로드 및 로컬 저장
- **태그 및 장르**: 상세한 게임 분류 정보 수집
- **판매량 및 평점**: 실시간 인기도 정보 제공

### 📊 고급 검색 및 정렬
- **다중 검색 필드**: 제목, 제작자, 식별자, 장르별 세밀한 검색
- **실시간 검색**: 입력과 동시에 결과 필터링 (디바운싱 적용)
- **다양한 정렬 옵션**: 제목, 제작자, 추가일, 크기, 평점 등으로 정렬
- **페이지네이션**: 대량 데이터 효율적 표시 (10~200개/페이지)

### 🎮 게임 실행 및 관리
- **원클릭 실행**: 게임 폴더 또는 실행 파일 직접 실행
- **세이브 폴더 관리**: 게임별 세이브 데이터 경로 저장 및 관리
- **플레이타임 추적**: 게임별 플레이 시간 및 마지막 플레이 시간 기록
- **사용자 메모**: 개인적인 게임 노트 및 평가 저장

### 🎨 사용자 인터페이스
- **라이트/다크 테마**: 시스템 테마에 맞춘 자동 전환
- **반응형 디자인**: 윈도우 크기에 따른 동적 레이아웃 조정
- **한글 폰트 최적화**: Noto Sans KR 폰트 적용으로 한글 가독성 향상
- **직관적 네비게이션**: 명확한 아이콘과 단축키 지원

## 🛠 기술 스택

### 코어 기술
- **언어**: C# (.NET 9.0)
- **UI 프레임워크**: WPF (Windows Presentation Foundation)
- **아키텍처 패턴**: MVVM (Model-View-ViewModel)
- **의존성 주입**: Microsoft.Extensions.DependencyInjection

### 데이터베이스 및 스토리지
- **데이터베이스**: SQLite (Microsoft.Data.Sqlite 9.0.4)
- **JSON 직렬화**: System.Text.Json
- **파일 시스템**: .NET File I/O API

### 웹 기술
- **웹 스크래핑**: HtmlAgilityPack 1.12.1
- **CSS 셀렉터**: HtmlAgilityPack.CssSelectors.NetCore 1.2.1
- **HTTP 클라이언트**: HttpClient (비동기 요청)

### 성능 최적화
- **비동기 처리**: async/await 패턴 전반 적용
- **메모리 관리**: IDisposable 패턴 구현
- **UI 응답성**: 백그라운드 작업 및 진행률 표시

## 📁 프로젝트 구조

```
DLGameViewer/
├── Models/                      # 데이터 모델
│   └── GameInfo.cs             # 게임 정보 엔티티 (INotifyPropertyChanged 구현)
├── ViewModels/                  # MVVM 뷰모델
│   ├── MainViewModel.cs        # 메인 화면 로직 (페이지네이션, 검색, 정렬)
│   ├── GameInfoViewModel.cs    # 게임 상세 정보 관리
│   ├── ScanResultViewModel.cs  # 스캔 진행 상황 관리
│   ├── ViewModelBase.cs        # 공통 뷰모델 기반 클래스
│   └── RelayCommand.cs         # ICommand 구현체
├── Services/                    # 비즈니스 로직 서비스
│   ├── DatabaseService.cs      # SQLite 데이터베이스 CRUD
│   ├── FolderScannerService.cs # 폴더 스캔 및 식별자 추출
│   ├── WebMetadataService.cs   # DLsite 웹 스크래핑
│   └── ImageService.cs         # 이미지 다운로드 및 저장
├── Interfaces/                  # 서비스 인터페이스
│   ├── IDatabaseService.cs
│   ├── IFolderScannerService.cs
│   ├── IWebMetadataService.cs
│   └── IImageService.cs
├── Views/                       # UI 정의
│   ├── MainWindow.xaml         # 메인 애플리케이션 윈도우
│   └── Dialogs/                # 대화상자 컬렉션
│       ├── GameInfoDialog.xaml    # 게임 상세 정보 편집
│       ├── ScanResultDialog.xaml  # 스캔 진행 상황 표시
│       └── ExecutableSelectionDialog.xaml # 실행 파일 선택
├── Helpers/                     # 유틸리티 클래스
│   ├── ThemeManager.cs         # 라이트/다크 테마 관리
│   ├── FolderScanHelper.cs     # 폴더 스캔 도우미
│   └── GameExecutionHelper.cs  # 게임 실행 도우미
├── Styles/                      # WPF 스타일 및 테마
├── Resources/                   # 리소스 파일
│   ├── Fonts/                  # 폰트 파일 (Noto Sans KR)
│   └── favicon.ico             # 애플리케이션 아이콘
└── Utils/                       # 유틸리티 및 컨버터
```

## 🏗 아키텍처 특징

### MVVM 패턴 구현
- **Model**: `GameInfo` 클래스로 데이터 구조 정의
- **View**: XAML 파일로 UI 선언적 정의
- **ViewModel**: 비즈니스 로직과 UI 상태 관리, `INotifyPropertyChanged` 구현

### 의존성 주입 (DI)
- 서비스 레이어 인터페이스 기반 설계
- `App.xaml.cs`에서 DI 컨테이너 구성
- 테스트 가능한 모듈식 아키텍처

### 비동기 프로그래밍
- 모든 I/O 작업 비동기 처리
- UI 스레드 블로킹 방지
- CancellationToken을 통한 작업 취소 지원

### 데이터베이스 설계
```sql
-- 주요 테이블 구조
Games (
    Id INTEGER PRIMARY KEY,
    Identifier TEXT UNIQUE,
    Title TEXT,
    Creator TEXT,
    GameType TEXT,
    Genres TEXT (JSON),
    SalesCount INTEGER,
    Rating TEXT,
    RatingCount INTEGER,
    FolderPath TEXT,
    SaveFolderPath TEXT,
    ExecutableFiles TEXT (JSON),
    DateAdded DATETIME,
    LastPlayed DATETIME,
    ReleaseDate DATETIME,
    FileSize TEXT,
    PlayTime TEXT,
    UserMemo TEXT
)
```

## 🚀 설치 및 실행

### 시스템 요구사항
- **운영체제**: Windows 10 1809 이상 (Windows 11 권장)
- **프레임워크**: .NET 9.0 런타임
- **메모리**: 최소 512MB RAM
- **저장공간**: 100MB 이상 여유 공간

### 실행 방법
1. [Releases](https://github.com/your-repo/releases) 페이지에서 최신 버전 다운로드
2. 압축 해제 후 `DLGameViewer.exe` 실행
3. 첫 실행 시 폴더 스캔 경로 설정
4. '스캔 시작' 버튼으로 게임 라이브러리 구축

### 개발 환경 설정
```bash
# 저장소 클론
git clone https://github.com/your-repo/DLGameViewer.git
cd DLGameViewer

# 의존성 복원
dotnet restore

# 빌드
dotnet build

# 실행
dotnet run
```

## 📝 사용법

### 1. 초기 설정
- 애플리케이션 실행 후 '폴더 추가' 버튼 클릭
- DLsite 게임이 저장된 로컬 폴더 선택
- '스캔 시작'으로 자동 메타데이터 수집 시작

### 2. 게임 관리
- **검색**: 상단 검색창에서 제목/제작자/식별자로 검색
- **정렬**: 드롭다운에서 정렬 기준 선택 (제목, 날짜, 크기 등)
- **페이지 이동**: 하단 페이지네이션으로 대량 데이터 탐색

### 3. 게임 실행
- 게임 선택 후 '게임 실행' 버튼 클릭
- 여러 실행 파일이 있을 경우 선택 다이얼로그 표시
- 실행 시 자동으로 플레이타임 추적 시작

### 4. 상세 정보 관리
- 게임 더블클릭으로 상세 정보 창 열기
- 메타데이터 수동 편집 및 사용자 메모 추가
- 세이브 폴더 경로 설정

## 🔄 향후 계획

### Phase 1: 사용자 경험 개선
- [ ] 고급 필터링 시스템 (출시일, 파일 크기, 평점 범위)
- [ ] 게임 태그 시스템 및 사용자 정의 태그
- [ ] 즐겨찾기 및 위시리스트 기능
- [ ] 게임 리뷰 및 개인 평점 시스템

### Phase 2: 확장 기능
- [ ] 압축 파일 지원 (ZIP, RAR, 7z)
- [ ] 게임 스크린샷 갤러리
- [ ] 온라인 백업 및 동기화
- [ ] 플러그인 시스템

### Phase 3: 성능 및 최적화
- [ ] 가상화된 리스트 뷰 (수만 개 게임 처리)
- [ ] 이미지 지연 로딩 및 캐싱 최적화
- [ ] 멀티스레드 스캔 성능 향상
- [ ] 메모리 사용량 최적화

### Phase 4: 디자인 및 접근성
- [ ] 머티리얼 디자인 UI 개선
- [ ] 접근성 향상 (스크린 리더 지원)
- [ ] 다국어 지원 (영어, 일본어)
- [ ] 사용자 정의 테마 및 레이아웃

## 🤝 기여하기

### 개발 가이드라인
1. 코드 스타일: [C# 코딩 컨벤션](https://docs.microsoft.com/ko-kr/dotnet/csharp/fundamentals/coding-style/) 준수
2. MVVM 패턴 유지
3. 모든 I/O 작업 비동기 처리
4. 단위 테스트 작성 권장

### 버그 리포트 및 기능 요청
- [Issues](https://github.com/your-repo/issues) 페이지에서 버그 신고
- 재현 단계와 환경 정보 포함 필수
- 기능 요청 시 사용 시나리오 상세 설명

## 📄 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

## ⚠️ 주의사항

- 이 애플리케이션은 **개인 용도로만** 사용하는 것을 권장합니다
- DLsite 콘텐츠 사용 시 해당 사이트의 이용약관을 준수해야 합니다
- 웹 스크래핑 기능은 DLsite 웹사이트 구조 변경 시 영향을 받을 수 있습니다
- 로컬에 저장된 콘텐츠의 저작권은 원 저작자에게 있습니다

## 🙏 감사의 말

- **HtmlAgilityPack**: 웹 스크래핑 라이브러리 제공
- **Microsoft**: .NET 및 WPF 프레임워크
- **SQLite**: 경량 데이터베이스 엔진
- **Noto Fonts**: 한글 폰트 지원

---

**DLGameViewer**로 더 나은 게임 라이브러리 관리 경험을 시작해보세요! 🎮 