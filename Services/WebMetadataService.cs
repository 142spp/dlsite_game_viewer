using System;
using System.Diagnostics;
using System.IO; // Path 클래스 사용을 위해 추가
using System.Net.Http;
using System.Threading.Tasks;
using DLGameViewer.Models;
using DLGameViewer.Interfaces;
using HtmlAgilityPack; // HtmlAgilityPack 사용
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;
using System.Net;


namespace DLGameViewer.Services {
    internal class ProductApiDetails {
        [JsonPropertyName("maker_id")]
        public string? MakerId { get; set; }

        [JsonPropertyName("dl_count")]
        public int DlCount { get; set; }

        [JsonPropertyName("wishlist_count")]
        public int WishlistCount { get; set; }

        [JsonPropertyName("rate_average_2dp")]
        public double RateAverage2dp { get; set; }

        [JsonPropertyName("rate_count")]
        public int RateCount { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("regist_date")]
        public string? RegistDate { get; set; }

    }

    public class WebMetadataService : IWebMetadataService {
        private readonly HttpClient _httpClient;
        private readonly IImageService _imageService; // ImageService 인터페이스로 변경
        private const string _dlsiteApiUrl = "https://www.dlsite.com/maniax/product/info/ajax?product_id={0}";
        private const string _dlsiteHtmlUrl = "https://www.dlsite.com/maniax/work/=/product_id/{0}.html/?locale=ko_KR";

        public WebMetadataService(IImageService imageService) { // 생성자에서 IImageService 주입
            _httpClient = new HttpClient();
            // 웹사이트에서 차단되는 것을 방지하기 위해 사용자 에이전트 설정
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _imageService = imageService; // 주입받은 IImageService 할당
        }


        public async Task<GameInfo> FetchMetadataAsync(string identifier) {
            if (string.IsNullOrWhiteSpace(identifier)) {
                return new GameInfo { Identifier = identifier };
            }

            // dlsite.com URL 구성 (maniax 카테고리 기준, 한국어 로캘 추가)
            string apiUrl = string.Format(_dlsiteApiUrl, identifier);
            string htmlUrl = string.Format(_dlsiteHtmlUrl, identifier);
            GameInfo gameInfo = new GameInfo { Identifier = identifier };

            // API 호출 시도
            try {
                string apiContent = await _httpClient.GetStringAsync(apiUrl);
                if (string.IsNullOrWhiteSpace(apiContent)) {
                    return gameInfo;
                }

                var apiResponse = JsonSerializer.Deserialize<Dictionary<string, ProductApiDetails>>(apiContent);
                if (apiResponse != null && apiResponse.TryGetValue(identifier, out ProductApiDetails productDetails)) {
                    gameInfo.SalesCount = productDetails.DlCount; 
                    gameInfo.Rating = productDetails.RateAverage2dp.ToString("F2", CultureInfo.InvariantCulture);
                    gameInfo.RatingCount = productDetails.RateCount;
                    
                    if (!string.IsNullOrWhiteSpace(productDetails.RegistDate)) {
                        if (DateTime.TryParseExact(productDetails.RegistDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime releaseDate)) {
                            gameInfo.ReleaseDate = DateTime.SpecifyKind(releaseDate, DateTimeKind.Utc);
                        }
                    }
                } else{
                    throw new Exception("API에서 제품 정보 찾을 수 없음");
                }
            } catch (HttpRequestException ex) {
                Console.WriteLine($"HTTP 요청 오류: {ex.Message}");
                return gameInfo;
            } catch (JsonException ex) {
                Console.WriteLine($"JSON 파싱 오류: {ex.Message}");
                return gameInfo;
            } catch (Exception ex) {
                Console.WriteLine($"예상치 못한 오류가 발생했습니다 : {identifier} : {ex.Message}");
                return gameInfo;
            }

            // HTML 호출 시도
            try {
                string htmlContent = await _httpClient.GetStringAsync(htmlUrl);
                if (string.IsNullOrWhiteSpace(htmlContent)) {
                    return gameInfo;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                // 파싱 시도
                gameInfo.Title = ParseTitle(htmlDoc);
                gameInfo.Creator = ParseCreator(htmlDoc);
                gameInfo.GameType = ParseGameType(htmlDoc);
                gameInfo.Genres = ParseGenres(htmlDoc);
                gameInfo.FileSize = ParseFileSize(htmlDoc);

                List<string> imageUrls = ParseImageUrls(htmlDoc);
                if (imageUrls.Count > 0) {
                    gameInfo.CoverImageUrl = imageUrls.First() ?? string.Empty;
                }

                foreach (var imageUrl in imageUrls) {
                    try{
                    if (!string.IsNullOrWhiteSpace(imageUrl)) {
                        // URL에서 파일 이름 추출 시도 (간단한 방식)
                        string fileName = "image.jpg";
                        try {
                            Uri uri = new Uri(imageUrl);
                            fileName = Path.GetFileName(uri.LocalPath);
                        } catch (UriFormatException) {
                            throw new Exception($"이미지 URL로부터 파일이름을 추출하지 못했습니다 : {imageUrl}");
                        }
                        try{
                            string localImagePath = await _imageService.DownloadAndSaveImageAsync(imageUrl, identifier, fileName);
                            if (string.IsNullOrWhiteSpace(localImagePath)) {
                                throw new Exception();
                            }
                            gameInfo.LocalImagePath = localImagePath;
                            gameInfo.CoverImagePath = Path.Combine(localImagePath, $"{identifier}_img_main.jpg");
                        } catch (Exception ex) {
                            throw new Exception($"이미지 다운로드 또는 저장에 실패했습니다 : {imageUrl} : {ex.Message}");
                        }
                    }
                    } catch (Exception) {
                        continue;
                    }
                }

                return gameInfo;
            } catch (HttpRequestException ex) {
                Console.WriteLine($"HTTP 요청 오류: {ex.Message}");
                return new GameInfo { Identifier = identifier };
            } catch (Exception ex) {
                Console.WriteLine($"예상치 못한 오류가 발생했습니다 : {identifier} : {ex.Message}");
                return new GameInfo { Identifier = identifier };
            }
        }
        private string ParseTitle(HtmlDocument htmlDoc) {
            var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[@id='work_name']");
            var titleText = HtmlEntity.DeEntitize(titleNode?.InnerText.Trim() ?? string.Empty);
            return titleText ?? string.Empty;
        }

        private string ParseCreator(HtmlDocument htmlDoc) {
            var creatorNode = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='work_maker']//span[@class='maker_name']/a");
            var creatorText = HtmlEntity.DeEntitize(creatorNode?.InnerText.Trim() ?? string.Empty);
            return creatorText ?? string.Empty;
        }

        private string ParseGameType(HtmlDocument htmlDoc) {
            var gameTypeNode = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='work_outline']//div[@id='category_type']/a/span[1]");
            var gameTypeText = HtmlEntity.DeEntitize(gameTypeNode?.InnerText.Trim() ?? string.Empty);
            return gameTypeText ?? string.Empty;
        }

        private string ParseFileSize(HtmlDocument htmlDoc) {
            var fileSizeNode = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='work_outline']//th[normalize-space(.)='파일 용량']/following-sibling::td");
            var fileSizeText = HtmlEntity.DeEntitize(fileSizeNode?.InnerText.Trim() ?? string.Empty);
            // 파일 크기 문자열에서 숫자와 단위만 추출
            if (!string.IsNullOrWhiteSpace(fileSizeText)) {
                var match = Regex.Match(fileSizeText, @"(\d+(?:\.\d+)?)\s*([KMG]B)", RegexOptions.IgnoreCase);
                if (match.Success) {
                    fileSizeText = $"{match.Groups[1].Value} {match.Groups[2].Value.ToUpper()}";
                }
            }
            return fileSizeText ?? string.Empty;
        }

        private List<string> ParseGenres(HtmlDocument htmlDoc) {
            var genres = new List<string>();
            // 장르 텍스트를 포함하는 th 태그의 다음 td 요소 내부의 div.main_genre 내의 a 태그들을 선택
            var genreNodes = htmlDoc.DocumentNode.SelectNodes("//table[@id='work_outline']//div[@class='main_genre']/a");
            if (genreNodes != null) {
                foreach (var node in genreNodes) {
                    var genreText = HtmlEntity.DeEntitize(node.InnerText.Trim());
                    genres.Add(genreText ?? string.Empty);
                }
            }
            return genres;
        }

        private List<string> ParseImageUrls(HtmlDocument htmlDoc) {
            List<string> imageUrls = new List<string>();

            // 우선 순위 1: product-slider-data 내부의 data-src (더 명확한 메인 이미지 경로로 보임)
            var ImageDivNodes = htmlDoc.DocumentNode.SelectNodes("//div[@id='work_left']//div[contains(@class, 'product-slider-data')]/div");
            if (ImageDivNodes != null) {
                foreach (var imageDivNode in ImageDivNodes) {
                    if (imageDivNode != null) {
                        var imageUrl = imageDivNode.GetAttributeValue("data-src", string.Empty);
                        if (string.IsNullOrWhiteSpace(imageUrl)) {
                            continue;
                        }
                        if (imageUrl.StartsWith("//")) {
                            imageUrl = "https:" + imageUrl;
                        }
                        imageUrls.Add(imageUrl);
                    }
                }
            }

            return imageUrls ?? new List<string>();
        }

        private string ParseDescription(HtmlDocument htmlDoc) {
            var descriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@itemprop='description']//div[@class='work_parts_area']");
            // HTML 태그를 제거하고 순수 텍스트만 얻거나, InnerHtml로 HTML을 그대로 가져올 수 있습니다.
            // 여기서는 InnerText를 사용하여 텍스트만 가져오고, 줄바꿈 등을 유지하기 위해 NormalizeWhitespace() 등을 사용할 수 있으나, 우선 Trim()만 적용합니다.
            var descriptionText = HtmlEntity.DeEntitize(descriptionNode?.InnerText?.Trim() ?? string.Empty);

            return descriptionText ?? string.Empty;
        }

        // 추가적으로 필요한 다른 정보(평점, 태그 등) 파싱 메서드들을 여기에 추가할 수 있습니다.
    }
}