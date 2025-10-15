namespace ProjectOrderNumberSystem.Services
{
    public interface IBoardApiService
    {
        /// <summary>
        /// 案件番号から案件情報を取得
        /// </summary>
        Task<dynamic?> GetProjectByNumberAsync(string caseNumber);

        /// <summary>
        /// 案件一覧を取得
        /// </summary>
        Task<List<dynamic>> SearchProjectsAsync(int perPage = 100, bool filterEmptyManagementNo = true);

        /// <summary>
        /// 管理番号を更新
        /// </summary>
        Task<bool> UpdateProjectManagementNumberAsync(int projectId, string managementNumber);
    }
}
