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
                _logger.LogInformation($"[BoardApi] 案件番号で検索: {caseNumber}");

                if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiToken))
                {
                    _logger.LogWarning("[BoardApi] Board API認証情報が設定されていません");
                    return null;
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiToken}");

                var url = $"{BaseUrl}/projects?project_no={caseNumber}";
                _logger.LogInformation($"[BoardApi] リクエストURL: {url}");

                var response = await client.GetAsync(url);
                _logger.LogInformation($"[BoardApi] HTTPステータス: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"[BoardApi] レスポンス: {content}");

                    var result = JsonConvert.DeserializeObject<List<dynamic>>(content);

                    if (result != null && result.Count > 0)
                    {
                        var project = result[0];
                        _logger.LogInformation($"[BoardApi] 案件を発見: ID={project.id}, 案件名={project.name}");

                        // 見積り金額フィールドの確認ログ
                        _logger.LogInformation($"[BoardApi] 金額フィールド確認 - estimate_amount={project.estimate_amount}, quotation_amount={project.quotation_amount}, amount={project.amount}, budget={project.budget}, price={project.price}");

                        return project; // 配列の最初の要素を返す
                    }

                    _logger.LogWarning($"[BoardApi] 案件番号 {caseNumber} が見つかりません");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[BoardApi] エラーレスポンス: {response.StatusCode} - {errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[BoardApi] 案件番号 {caseNumber} の取得エラー");
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
                _logger.LogInformation($"[BoardApi] 案件一覧取得開始 (perPage={perPage})");

                if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiToken))
                {
                    _logger.LogWarning("[BoardApi] Board API認証情報が設定されていません (ApiKey={0}, ApiToken={1})",
                        string.IsNullOrEmpty(ApiKey) ? "null" : "設定あり",
                        string.IsNullOrEmpty(ApiToken) ? "null" : "設定あり");
                    return new List<dynamic>();
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiToken}");

                var url = $"{BaseUrl}/projects?per_page={perPage}&sort=-created_at";
                _logger.LogInformation($"[BoardApi] リクエストURL: {url}");

                var response = await client.GetAsync(url);
                _logger.LogInformation($"[BoardApi] HTTPステータス: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"[BoardApi] レスポンス長: {content?.Length ?? 0} bytes");

                    var result = JsonConvert.DeserializeObject<List<dynamic>>(content);
                    _logger.LogInformation($"[BoardApi] デシリアライズ結果: {result?.Count ?? 0} 件");

                    if (result != null && filterEmptyManagementNo)
                    {
                        // 管理番号が空のものをフィルタ（実装は要調整）
                        return result;
                    }

                    return result ?? new List<dynamic>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[BoardApi] エラーレスポンス: {response.StatusCode} - {errorContent}");
                }

                return new List<dynamic>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BoardApi] 案件一覧取得エラー");
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
                _logger.LogInformation($"[BoardApi] 管理番号更新開始: ProjectId={projectId}, ManagementNumber={managementNumber}");

                if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiToken))
                {
                    _logger.LogWarning("[BoardApi] Board API認証情報が設定されていません");
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
                _logger.LogInformation($"[BoardApi] リクエストボディ: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var url = $"{BaseUrl}/projects/{projectId}";
                _logger.LogInformation($"[BoardApi] PUT {url}");

                var response = await client.PutAsync(url, content);
                _logger.LogInformation($"[BoardApi] HTTPステータス: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"[BoardApi] 管理番号更新成功: ProjectId={projectId}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[BoardApi] 管理番号更新失敗: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[BoardApi] 管理番号更新エラー (ProjectId: {projectId})");
                return false;
            }
        }
    }
}
