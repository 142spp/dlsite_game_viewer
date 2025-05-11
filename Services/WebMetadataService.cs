using System;
using System.IO; // Path 클래스 사용을 위해 추가
using System.Net.Http;
using System.Threading.Tasks;
using DLGameViewer.Models;
using HtmlAgilityPack; // HtmlAgilityPack 사용

namespace DLGameViewer.Services
{
    public class WebMetadataService
    {
        private readonly HttpClient _httpClient;
        private readonly ImageService _imageService; // ImageService 추가

        public WebMetadataService(ImageService imageService) // 생성자에서 ImageService 주입
        {
            _httpClient = new HttpClient();
            // 웹사이트에서 차단되는 것을 방지하기 위해 사용자 에이전트 설정
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _imageService = imageService; // 주입받은 ImageService 할당
        }

        public async Task<GameInfo?> FetchMetadataAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return null;
            }

            // dlsite.com URL 구성 (maniax 카테고리 기준, 한국어 로캘 추가)
            string url = $"https://www.dlsite.com/maniax/work/=/product_id/{identifier}.html/?locale=ko_KR";

            try
            {
                string htmlContent = await _httpClient.GetStringAsync(url);
                if (string.IsNullOrWhiteSpace(htmlContent))
                {
                    Console.WriteLine($"HTML 콘텐츠가 비어있습니다 : {identifier}");
                    return null;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                GameInfo gameInfo = new GameInfo
                {
                    Identifier = identifier,
                    CoverImageUrl = ParseCoverImageUrl(htmlDoc),
                    Title = ParseTitle(htmlDoc),
                    Creator = ParseCreator(htmlDoc),
                    Genres = ParseGenres(htmlDoc),
                    AdditionalMetadata = ParseDescription(htmlDoc)
                };

                // CoverImageUrl이 유효하면 이미지 다운로드 및 저장 시도
                if (!string.IsNullOrWhiteSpace(gameInfo.CoverImageUrl)){
                    // URL에서 파일 이름 추출 시도 (간단한 방식)
                    string fileName = "cover.jpg"; // 기본 파일 이름
                    try{
                        Uri uri = new Uri(gameInfo.CoverImageUrl);
                        string extractedFileName = Path.GetFileName(uri.LocalPath);
                        if (!string.IsNullOrWhiteSpace(extractedFileName)){
                            fileName = extractedFileName;
                        }
                    }
                    catch (UriFormatException){
                        Console.WriteLine($"이미지 URL로부터 파일이름을 추출하지 못했습니다 : {gameInfo.CoverImageUrl}");
                    }
                    
                    // 이미지 확장자가 없는 경우 기본 .jpg 추가 (더 견고한 로직이 필요할 수 있음)
                    if (!Path.HasExtension(fileName) || Path.GetExtension(fileName).Length < 2){
                        fileName = Path.ChangeExtension(fileName, ".jpg");
                    }

                    string? localImagePath = await _imageService.DownloadAndSaveImageAsync(gameInfo.CoverImageUrl, identifier, fileName);
                    if (!string.IsNullOrWhiteSpace(localImagePath)){
                        gameInfo.LocalImagePath = localImagePath;
                    }
                    else{
                        Console.WriteLine($"이미지 다운로드 또는 저장에 실패했습니다 : {identifier} : {gameInfo.CoverImageUrl}");
                    }
                }

                return gameInfo; 
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTML 가져오기에 실패했습니다 : {identifier} : {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예상치 못한 오류가 발생했습니다 : {identifier} : {ex.Message}");
                return null;
            }
        }
        private string ParseTitle(HtmlDocument htmlDoc)
        {
            var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[@id='work_name']");
            return titleNode?.InnerText.Trim() ?? string.Empty;
        }

        private string ParseCreator(HtmlDocument htmlDoc)
        {
            var creatorNode = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='work_maker']//span[@class='maker_name']/a");
            return creatorNode?.InnerText.Trim() ?? string.Empty;
        }
        
        private List<string> ParseGenres(HtmlDocument htmlDoc)
        {
            var genres = new List<string>();
            // 장르 텍스트를 포함하는 th 태그의 다음 td 요소 내부의 div.main_genre 내의 a 태그들을 선택
            var genreNodes = htmlDoc.DocumentNode.SelectNodes("//table[@id='work_outline']//div[@class='main_genre']/a");
            Console.WriteLine($"genreNodes: {genreNodes}");
            if (genreNodes != null)
            {
                foreach (var node in genreNodes)
                {
                    genres.Add(node.InnerText.Trim());
                }
            }
            return genres;
        }

        private string ParseCoverImageUrl(HtmlDocument htmlDoc) // identifier 인자 제거
        {
            string? imageUrl = null;

            // 우선 순위 1: product-slider-data 내부의 data-src (더 명확한 메인 이미지 경로로 보임)
            var mainImageDivNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='work_left']//div[contains(@class, 'product-slider-data')]/div[1]");
            if (mainImageDivNode != null)
            {
                imageUrl = mainImageDivNode.GetAttributeValue("data-src", string.Empty);
            }

            // 우선 순위 2: itemprop="image" 속성을 가진 img 태그의 srcset 또는 src
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                var imageNode = htmlDoc.DocumentNode.SelectSingleNode("//img[@itemprop='image']");
                imageUrl = imageNode?.GetAttributeValue("srcset", string.Empty) ?? imageNode?.GetAttributeValue("src", string.Empty);

                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    // srcset은 여러 URL과 크기 정보를 포함할 수 있으므로, 첫 번째 URL만 가져오거나 가장 큰 이미지를 선택하는 로직 필요
                    var parts = imageUrl.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var firstPart = parts[0].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (firstPart.Length > 0)
                        {
                            imageUrl = firstPart[0];
                        }
                    }
                }
            }
            
            // URL이 //로 시작하면 현재 스킴(https)을 붙여줍니다.
            if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.StartsWith("//"))
            {
                return "https:" + imageUrl;
            }

            return imageUrl?.Trim() ?? string.Empty;
        }

        private string ParseDescription(HtmlDocument htmlDoc)
        {
            var descriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@itemprop='description']//div[@class='work_parts_area']");
            // HTML 태그를 제거하고 순수 텍스트만 얻거나, InnerHtml로 HTML을 그대로 가져올 수 있습니다.
            // 여기서는 InnerText를 사용하여 텍스트만 가져오고, 줄바꿈 등을 유지하기 위해 NormalizeWhitespace() 등을 사용할 수 있으나, 우선 Trim()만 적용합니다.
            var description = HtmlEntity.DeEntitize(descriptionNode?.InnerText?.Trim() ?? string.Empty);

            return description ?? string.Empty;
        }

        // 추가적으로 필요한 다른 정보(평점, 태그 등) 파싱 메서드들을 여기에 추가할 수 있습니다.
    }
} 