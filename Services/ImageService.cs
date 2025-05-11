using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DLGameViewer.Services
{
    public class ImageService
    {
        private readonly string _baseImageSavePath;
        private static readonly HttpClient _httpClient = new HttpClient();

        public ImageService()
        {
            // 애플리케이션 실행 파일이 있는 디렉토리를 기준으로 Data/Images 폴더 경로 설정
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // WPF 앱에서는 실행 파일이 bin/Debug/netX.X-windows 에 위치하므로, 프로젝트 루트로 이동
            string projectRootPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            if (!Directory.Exists(Path.Combine(projectRootPath, "Data")) && Directory.Exists(baseDir)) // 개발 중 VSCode 실행 경로 대응
            {
                projectRootPath = baseDir; // 터미널에서 직접 dotnet run 하는 경우 BaseDirectory가 프로젝트 루트일 수 있음
            }
            
            _baseImageSavePath = Path.Combine(projectRootPath, "Data", "Images");
            Directory.CreateDirectory(_baseImageSavePath); // 기본 이미지 저장 폴더가 없으면 생성
        }

        public async Task<string?> DownloadAndSaveImageAsync(string imageUrl, string identifier, string fileName)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(fileName))
            {
                // Log error or throw argument exception
                Console.WriteLine("이미지 URL, 식별자, 또는 파일이름이 비어있습니다.");
                return null;
            }

            try
            {
                // 식별자별 폴더 경로 생성
                string identifierFolderPath = Path.Combine(_baseImageSavePath, identifier);
                Directory.CreateDirectory(identifierFolderPath); // 식별자 폴더가 없으면 생성

                string localImagePath = Path.Combine(identifierFolderPath, fileName);

                // HTTP GET 요청을 사용하여 이미지 다운로드
                byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                // 파일에 이미지 바이트 쓰기
                await File.WriteAllBytesAsync(localImagePath, imageBytes);

                Console.WriteLine($"이미지 다운로드 및 저장됨 : {localImagePath}");
                return localImagePath; // 저장된 전체 경로 반환
            }
            catch (HttpRequestException ex)
            {
                // 네트워크 오류 또는 잘못된 URL 처리
                Console.WriteLine($"이미지 다운로드에 실패했습니다 : {imageUrl} : {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                // 파일 시스템 오류 처리 (예: 디스크 공간 부족, 권한 문제)
                Console.WriteLine($"이미지 저장에 실패했습니다 : {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // 기타 예외 처리
                Console.WriteLine($"예기치 못한 오류가 발생했습니다 : {ex.Message}");
                return null;
            }
        }
    }
} 