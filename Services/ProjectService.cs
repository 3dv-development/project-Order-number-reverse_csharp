using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectOrderNumberSystem.Data;
using ProjectOrderNumberSystem.Models;

namespace ProjectOrderNumberSystem.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;

        public ProjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 受注番号を生成
        /// </summary>
        public async Task<string> GenerateProjectNumberAsync(string category)
        {
            var currentYear = DateTime.UtcNow.Year % 100; // 西暦下2桁
            var yearPrefix = $"{currentYear:D2}{category}";

            // 同じ年・カテゴリの最大連番を取得
            var lastProject = await _context.Projects
                .Where(p => p.ProjectNumber.StartsWith(yearPrefix))
                .OrderByDescending(p => p.ProjectNumber)
                .FirstOrDefaultAsync();

            int newNumber;
            if (lastProject != null)
            {
                // 最後の3桁（連番）を取得してインクリメント
                var lastNumberStr = lastProject.ProjectNumber.Substring(4, 3);
                var lastNumber = int.Parse(lastNumberStr);
                newNumber = lastNumber + 1;
            }
            else
            {
                // 初めての場合は001から
                newNumber = 1;
            }

            // 7桁の受注番号を生成
            var projectNumber = $"{yearPrefix}{newNumber:D3}";

            return projectNumber;
        }

        /// <summary>
        /// プロジェクトを作成
        /// </summary>
        public async Task<Project> CreateProjectAsync(Project project, string editorId, string editorName)
        {
            // 受注番号を生成
            project.ProjectNumber = await GenerateProjectNumberAsync(project.Category);
            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;

            // Deadlineフィールドをローカル時刻からUTCに変換（Kindが指定されていない場合）
            if (project.Deadline.Kind == DateTimeKind.Unspecified)
            {
                project.Deadline = DateTime.SpecifyKind(project.Deadline, DateTimeKind.Utc);
            }
            else if (project.Deadline.Kind == DateTimeKind.Local)
            {
                project.Deadline = project.Deadline.ToUniversalTime();
            }

            _context.Projects.Add(project);

            // 編集履歴を記録
            var history = new EditHistory
            {
                Project = project,
                EditorId = editorId,
                EditorName = editorName,
                EditType = "create",
                Changes = JsonConvert.SerializeObject(new { action = "新規作成" }),
                EditedAt = DateTime.UtcNow
            };

            _context.EditHistories.Add(history);

            await _context.SaveChangesAsync();

            return project;
        }

        /// <summary>
        /// プロジェクトを更新
        /// </summary>
        public async Task<Project> UpdateProjectAsync(Project project, string editorId, string editorName, Dictionary<string, object> changes)
        {
            project.UpdatedAt = DateTime.UtcNow;
            _context.Projects.Update(project);

            // 編集履歴を追加
            if (changes.Count > 0)
            {
                var history = new EditHistory
                {
                    ProjectId = project.Id,
                    EditorId = editorId,
                    EditorName = editorName,
                    EditType = "update",
                    Changes = JsonConvert.SerializeObject(changes),
                    EditedAt = DateTime.UtcNow
                };

                _context.EditHistories.Add(history);
            }

            await _context.SaveChangesAsync();

            return project;
        }

        /// <summary>
        /// プロジェクトを取得
        /// </summary>
        public async Task<Project?> GetProjectByNumberAsync(string projectNumber)
        {
            return await _context.Projects
                .Include(p => p.EditHistory)
                .FirstOrDefaultAsync(p => p.ProjectNumber == projectNumber);
        }

        /// <summary>
        /// プロジェクト一覧を取得
        /// </summary>
        public async Task<List<Project>> GetProjectsAsync(string? category = null, string? keyword = null)
        {
            var query = _context.Projects.AsQueryable();

            // カテゴリフィルタ
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            // キーワード検索
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p =>
                    p.ProjectNumber.Contains(keyword) ||
                    p.ProjectName.Contains(keyword) ||
                    p.ClientName.Contains(keyword) ||
                    p.StaffName.Contains(keyword));
            }

            // 最新順でソート
            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        /// <summary>
        /// プロジェクトを削除（編集履歴も一緒に削除）
        /// </summary>
        public async Task DeleteProjectAsync(string projectNumber)
        {
            var project = await _context.Projects
                .Include(p => p.EditHistory)
                .FirstOrDefaultAsync(p => p.ProjectNumber == projectNumber);

            if (project != null)
            {
                // 外部キー制約エラーを防ぐため、先に編集履歴を削除
                if (project.EditHistory != null && project.EditHistory.Any())
                {
                    _context.EditHistories.RemoveRange(project.EditHistory);
                }

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
        }
    }
}
