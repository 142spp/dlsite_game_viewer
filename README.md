# DLGameViewer (DL 게임 뷰어)

DLGameViewer는 DLsite 게임 및 콘텐츠를 로컬에서 관리하고 실행할 수 있는 Windows 데스크톱 애플리케이션입니다. 이 프로그램은 C#과 WPF로 개발되었으며, 사용자가 로컬 폴더에서 게임을 스캔하고, 메타데이터를 자동으로 가져오며, 게임을 분류하고 실행할 수 있는 직관적인 인터페이스를 제공합니다.

## 주요 기능

- **폴더 스캔**: 지정한 로컬 폴더를 재귀적으로 스캔하여 DLsite 콘텐츠를 자동으로 찾습니다
- **식별자 추출**: 폴더명에서 DLsite 식별자(RJ, VJ로 시작하는 번호)를 자동으로 추출합니다
- **메타데이터 수집**: 식별자를 기반으로 DLsite에서 제목, 제작자, 장르, 평점 등의 메타데이터를 자동으로 가져옵니다
- **이미지 관리**: 커버 이미지를 자동으로 다운로드하고 로컬에 저장합니다
- **실행 파일 관리**: 폴더 내에서 실행 가능한 파일(.exe)을 찾아 관리합니다
- **데이터베이스 관리**: 모든 정보는 SQLite 데이터베이스에 저장되어 효율적으로 관리됩니다
- **게임 목록 표시**: 수집된 게임 정보를 이미지와 함께 목록 형태로 표시합니다
- **검색 및 필터링**: 제목, 제작자, 식별자 등으로 게임을 검색하고 필터링할 수 있습니다
- **상세 정보 관리**: 개별 게임의 상세 정보를 확인하고 편집할 수 있습니다
- **실행 및 관리**: 게임 폴더를 열거나 실행 파일을 직접 실행할 수 있습니다
- **데이터 정리**: 더 이상 존재하지 않는 폴더의 항목을 데이터베이스에서 제거할 수 있습니다

## 기술 스택

- **언어**: C# (.NET 9.0)
- **UI 프레임워크**: WPF (Windows Presentation Foundation)
- **데이터베이스**: SQLite (Microsoft.Data.Sqlite)
- **웹 크롤링**: HtmlAgilityPack, HtmlAgilityPack.CssSelectors.NetCore
- **비동기 처리**: Task 기반 비동기 프로그래밍

## 구현 상세

이 프로젝트는 모던 C#의 기능과 WPF의 MVVM 패턴을 활용하여 구현되었습니다.

### 주요 컴포넌트 설명

1. **데이터 모델 (Models)**
   - `GameInfo`: 게임 정보를 표현하는 모델 클래스로, 식별자, 제목, 제작자, 이미지 경로 등의 정보를 포함합니다.

2. **서비스 레이어 (Services)**
   - `DatabaseService`: SQLite 데이터베이스 연결 및 CRUD 작업을 담당합니다. 모든 게임 정보가 이 서비스를 통해 로컬 데이터베이스에 저장됩니다.
   - `FolderScannerService`: 로컬 폴더를 재귀적으로 스캔하여 DLsite 콘텐츠의 식별자를 추출하고, 실행 파일을 찾는 기능을 제공합니다.
   - `WebMetadataService`: 추출된 식별자를 기반으로 DLsite 웹사이트에서 메타데이터를 수집합니다. HtmlAgilityPack을 활용한 웹 스크래핑 기법을 사용합니다.
   - `ImageService`: 웹에서 이미지를 다운로드하고 로컬에 저장하는 기능을 담당합니다.

3. **사용자 인터페이스 (UI)**
   - `MainWindow`: 주요 애플리케이션 윈도우로, 게임 목록 표시, 폴더 스캔, 게임 실행 등의 기능을 제공합니다.
   - `GameInfoDialog`: 게임 상세 정보를 표시하고 편집할 수 있는 다이얼로그 창입니다.
   - `ScanResultDialog`: 폴더 스캔 과정의 진행 상황을 실시간으로 표시하는 다이얼로그 창입니다.
   - `ExecutableSelectionDialog`: 여러 실행 파일이 있을 경우 사용자가 선택할 수 있는 다이얼로그 창입니다.

4. **유틸리티 (Utils)**
   - `Converters`: WPF 데이터 바인딩에 사용되는 값 변환기(Value Converter)를 제공합니다. 주로 이미지 경로를 이미지 소스로 변환하는 기능을 담당합니다.

### 비동기 처리

- 모든 시간이 오래 걸리는 작업(웹 요청, 파일 시스템 접근, 데이터베이스 작업)은 비동기로 처리되어 UI의 응답성을 유지합니다.
- C#의 `async/await` 패턴을 활용하여 코드의 가독성과 유지보수성을 높였습니다.

### 데이터 저장

- 모든 게임 정보는 SQLite 데이터베이스에 저장됩니다.
- 이미지는 로컬 파일 시스템에 저장되고, 데이터베이스에는 이미지 경로만 저장됩니다.
- 복잡한 데이터 구조(리스트 등)는 JSON 형식으로 직렬화하여 데이터베이스에 저장합니다.

### UI 디자인

- WPF의 데이터 바인딩을 활용하여 MVVM(Model-View-ViewModel) 패턴에 가까운 디자인을 구현했습니다.
- 반응형 UI로 설계되어 윈도우 크기 변경에 적절하게 대응합니다.
- 이미지 크기는 윈도우 크기에 맞게 자동으로 조정됩니다.

## 시스템 요구사항

- Windows OS
- .NET 9.0 런타임
- 최소 100MB의 하드 디스크 공간

## 설치 및 실행 방법

1. 최신 릴리스를 다운로드합니다.
2. 압축을 해제하고 원하는 위치에 저장합니다.
3. DLGameViewer.exe를 실행합니다.
4. 초기 설정에서 스캔할 폴더 경로를 추가합니다.
5. '스캔 시작' 버튼을 클릭하여 게임을 검색합니다.

## 프로젝트 구조

- **Models/**
  - `GameInfo.cs`: 게임 정보 모델 클래스
- **Services/**
  - `DatabaseService.cs`: SQLite 데이터베이스 관리
  - `FolderScannerService.cs`: 로컬 폴더 스캔 및 식별자 추출
  - `WebMetadataService.cs`: DLsite에서 메타데이터 수집
  - `ImageService.cs`: 이미지 다운로드 및 관리
- **Dialogs/**
  - `GameInfoDialog.xaml/.cs`: 게임 상세 정보 다이얼로그
  - `ScanResultDialog.xaml/.cs`: 스캔 결과 다이얼로그
  - `ExecutableSelectionDialog.xaml/.cs`: 실행 파일 선택 다이얼로그
- **Utils/**
  - `Converters.cs`: WPF용 데이터 변환기 (이미지 경로 등)
- **MainWindow.xaml/.cs**: 메인 애플리케이션 윈도우

## 향후 계획

### 사용자 편의성 증대
- 네비게이션 바 구현 (파일/도구/도움말 메뉴)
- 고급 정렬 기능 추가
- 고급 필터 및 검색 기능 향상
- 그리드 뷰 외에 리스트 형태로 보기 기능 추가
- 게임 상세정보 페이지에서 태그를 하이퍼링크 형태로 구현 및 관리 기능 개선
- 세이브 폴더 경로 저장 기능
- 플레이타임 추적 기능 구현
- 마지막 플레이 시간 저장 및 표시
- 압축 파일(ZIP, RAR 등) 지원 추가

### 최적화
- 대량의 게임 데이터 처리 시 성능 개선
- 메모리 사용량 최적화
- 이미지 캐싱 시스템 개선

### 심미성 증대
- 다크 모드 테마 추가
- 사용자 인터페이스 디자인 개선
- 다양한 테마 옵션 제공

## 라이선스

MIT 라이선스

## 주의사항

이 애플리케이션은 개인 용도로만 사용하는 것을 권장합니다. DLsite의 콘텐츠 사용에 관한 이용약관을 준수해 주세요. 