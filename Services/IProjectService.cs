using ProjectOrderNumberSystem.Models;

namespace ProjectOrderNumberSystem.Services
{
    public interface IProjectService
    {
        /// <summary>
        /// 受注番号を生成
        /// </summary>
        Task<string> GenerateProjectNumberAsync(string category);

        /// <summary>
        /// プロジェクトを作成
        /// </summary>
        Task<Project> CreateProjectAsync(Project project, string editorId, string editorName);

        /// <summary>
        /// プロジェクトを更新
        /// </summary>
        Task<Project> UpdateProjectAsync(Project project, string editorId, string editorName, Dictionary<string, object> changes);

        /// <summary>
        /// プロジェクトを取得
        /// </summary>
        Task<Project?> GetProjectByNumberAsync(string projectNumber);

        /// <summary>
        /// プロジェクト一覧を取得
        /// </summary>
        Task<List<Project>> GetProjectsAsync(string? category = null, string? keyword = null);

        /// <summary>
        /// プロジェクトを削除
        /// </summary>
        Task DeleteProjectAsync(string projectNumber);
    }
}
