using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrderNumberSystem.Data;
using ProjectOrderNumberSystem.Models;
using ProjectOrderNumberSystem.Services;

namespace ProjectOrderNumberSystem.Controllers
{
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProjectService _projectService;
        // private readonly IEmailService _emailService; // メール機能は一時無効化
        private readonly IBoardApiService _boardApiService;

        public ProjectController(
            ApplicationDbContext context,
            IProjectService projectService,
            // IEmailService emailService, // メール機能は一時無効化
            IBoardApiService boardApiService)
        {
            _context = context;
            _projectService = projectService;
            // _emailService = emailService; // メール機能は一時無効化
            _boardApiService = boardApiService;
        }

        // 採番フォーム表示
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            ViewData["Employees"] = employees;
            ViewData["EmployeeId"] = employeeId;

            return View();
        }

        // 採番実行
        [HttpPost]
        public async Task<IActionResult> Create(Project project)
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            var employeeName = HttpContext.Session.GetString("EmployeeName");

            if (string.IsNullOrEmpty(employeeId))
            {
                return Json(new { error = "ログインが必要です" });
            }

            try
            {
                // 担当者情報を取得
                var staff = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == project.StaffId);

                if (staff == null)
                {
                    return Json(new { error = "担当者が見つかりません" });
                }

                project.StaffName = staff.Name;

                // プロジェクトを作成
                var createdProject = await _projectService.CreateProjectAsync(
                    project,
                    employeeId,
                    employeeName ?? "Unknown"
                );

                // メール送信（一時的に無効化）
                // await _emailService.SendNotificationEmailAsync(createdProject);

                // Board連携: 案件Noがある場合、受注番号をboardに登録
                if (!string.IsNullOrEmpty(project.CaseNumber))
                {
                    try
                    {
                        var boardProject = await _boardApiService.GetProjectByNumberAsync(project.CaseNumber);
                        if (boardProject != null)
                        {
                            // 実装は要調整（boardProjectの構造に依存）
                        }
                    }
                    catch (Exception ex)
                    {
                        // Board連携エラーは無視
                        Console.WriteLine($"Board連携エラー: {ex.Message}");
                    }
                }

                return Json(new
                {
                    success = true,
                    projectNumber = createdProject.ProjectNumber,
                    message = $"受注番号 {createdProject.ProjectNumber} を採番しました"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"エラーが発生しました: {ex.Message}" });
            }
        }

        // 検索ページ
        [HttpGet]
        public async Task<IActionResult> Search(string? category, string? keyword)
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var projects = await _projectService.GetProjectsAsync(category, keyword);

            ViewData["Category"] = category;
            ViewData["Keyword"] = keyword;

            return View(projects);
        }

        // 詳細表示
        [HttpGet]
        public async Task<IActionResult> Detail(string projectNumber)
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var project = await _projectService.GetProjectByNumberAsync(projectNumber);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // 編集フォーム
        [HttpGet]
        public async Task<IActionResult> Edit(string projectNumber)
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var project = await _projectService.GetProjectByNumberAsync(projectNumber);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // 編集実行
        [HttpPost]
        public async Task<IActionResult> Edit(string projectNumber, Project updatedProject)
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            var employeeName = HttpContext.Session.GetString("EmployeeName");

            if (string.IsNullOrEmpty(employeeId))
            {
                return Json(new { error = "ログインが必要です" });
            }

            try
            {
                var project = await _projectService.GetProjectByNumberAsync(projectNumber);

                if (project == null)
                {
                    return Json(new { error = "案件が見つかりません" });
                }

                // 変更内容を記録
                var changes = new Dictionary<string, object>();

                if (updatedProject.CaseNumber != project.CaseNumber)
                {
                    changes["case_number"] = new { old = project.CaseNumber, _new = updatedProject.CaseNumber };
                    project.CaseNumber = updatedProject.CaseNumber;
                }

                if (updatedProject.ProjectName != project.ProjectName)
                {
                    changes["project_name"] = new { old = project.ProjectName, _new = updatedProject.ProjectName };
                    project.ProjectName = updatedProject.ProjectName;
                }

                if (updatedProject.ClientName != project.ClientName)
                {
                    changes["client_name"] = new { old = project.ClientName, _new = updatedProject.ClientName };
                    project.ClientName = updatedProject.ClientName;
                }

                if (updatedProject.Budget != project.Budget)
                {
                    changes["budget"] = new { old = project.Budget, _new = updatedProject.Budget };
                    project.Budget = updatedProject.Budget;
                }

                if (updatedProject.Deadline != project.Deadline)
                {
                    changes["deadline"] = new { old = project.Deadline, _new = updatedProject.Deadline };
                    project.Deadline = updatedProject.Deadline;
                }

                if (updatedProject.Remarks != project.Remarks)
                {
                    changes["remarks"] = new { old = project.Remarks, _new = updatedProject.Remarks };
                    project.Remarks = updatedProject.Remarks;
                }

                await _projectService.UpdateProjectAsync(project, employeeId, employeeName ?? "Unknown", changes);

                return Json(new
                {
                    success = true,
                    message = changes.Count > 0 ? "案件情報を更新しました" : "変更はありませんでした"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"エラーが発生しました: {ex.Message}" });
            }
        }

        // 削除
        [HttpPost]
        public async Task<IActionResult> Delete(string projectNumber)
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                await _projectService.DeleteProjectAsync(projectNumber);
                TempData["Success"] = $"案件（受注番号: {projectNumber}）を削除しました";
                return RedirectToAction("Search");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"削除に失敗しました: {ex.Message}";
                return RedirectToAction("Detail", new { projectNumber });
            }
        }
    }
}
