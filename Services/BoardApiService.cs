using Newtonsoft.Json;

namespace ProjectOrderNumberSystem.Services
{
    public class BoardApiService : IBoardApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BoardApiService> _logger;

        private string? ApiKey => _configuration["BoardApi:ApiKey"];
        private string? ApiToken => _configuration["BoardApi:ApiToken"];
        private const string BaseUrl = "https://api.the-board.jp/v1";

        public BoardApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<BoardApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 案件番号から案件情報を取得
        /// </summary>
        public async Task<dynamic?> GetProjectByNumberAsync(string caseNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiToken))
                {
                    _logger.LogWarning("Board API認証情報が設定されていません");
                    return null;
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiToken}");

                var response = await client.GetAsync($"{BaseUrl}/projects?project_no={caseNumber}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Board API エラー: 案件番号 {caseNumber}");
                return null;
            }
        }

        /// <summary>
        /// 案件一覧を取得
        /// </summary>
        public async Task<List<dynamic>> SearchProjectsAsync(int perPage = 100, bool filterEmptyManagementNo = true)
        {
            try
            {
                if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiToken))
                {
                    _logger.LogWarning("Board API認証情報が設定されていません");
                    return new List<dynamic>();
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiToken}");

                var url = $"{BaseUrl}/projects?per_page={perPage}&sort=-created_at";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<dynamic>>(content);

                    if (result != null && filterEmptyManagementNo)
                    {
                        // 管理番号が空のものをフィルタ（実装は要調整）
                        return result;
                    }

                    return result ?? new List<dynamic>();
                }

                return new List<dynamic>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Board API エラー: 案件一覧取得");
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// 管理番号を更新
        /// </summary>
        public async Task<bool> UpdateProjectManagementNumberAsync(int projectId, string managementNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiToken))
                {
                    _logger.LogWarning("Board API認証情報が設定されていません");
                    return false;
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiToken}");

                var data = new
                {
                    management_number = managementNumber
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{BaseUrl}/projects/{projectId}", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Board API エラー: 管理番号更新 (ProjectId: {projectId})");
                return false;
            }
        }
    }
}
