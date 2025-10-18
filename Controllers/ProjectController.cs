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

            // Board APIから管理番号が空の案件一覧を取得
            try
            {
                Console.WriteLine("[Board API] 案件取得を開始します");
                var boardProjects = await _boardApiService.SearchProjectsAsync(100, false);
                Console.WriteLine($"[Board API] {boardProjects?.Count ?? 0} 件の案件を取得しました");

                if (boardProjects != null && boardProjects.Count > 0)
                {
                    var emptyManagementProjects = boardProjects
                        .Where(p => string.IsNullOrEmpty(p.management_number?.ToString()))
                        .Select(p => new
                        {
                            Id = p.id?.ToString() ?? "",
                            ProjectNo = p.project_no?.ToString() ?? "",
                            Name = p.name?.ToString() ?? "",
                            ClientName = p.client?.name?.ToString() ?? "",
                            OrderStatus = p.order_status_name?.ToString() ?? "",
                            // 見積り金額フィールドの候補をすべて試す
                            Amount = p.estimate_amount?.ToString() ?? p.quotation_amount?.ToString() ?? p.estimated_amount?.ToString() ?? p.quote_amount?.ToString() ?? p.amount?.ToString() ?? p.budget?.ToString() ?? p.price?.ToString() ?? ""
                        })
                        .OrderByDescending(p => p.ProjectNo)
                        .ToList();

                    Console.WriteLine($"[Board API] 管理番号が空の案件: {emptyManagementProjects.Count} 件");
                    ViewBag.BoardProjects = emptyManagementProjects;
                    ViewBag.BoardApiStatus = "success";
                }
                else
                {
                    Console.WriteLine("[Board API] 取得した案件が0件です");
                    ViewBag.BoardProjects = new List<object>();
                    ViewBag.BoardApiStatus = "empty";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Board API] エラー: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[Board API] スタックトレース: {ex.StackTrace}");
                ViewBag.BoardProjects = new List<object>();
                ViewBag.BoardApiStatus = "error";
                ViewBag.BoardApiError = ex.Message;
            }

            return View();
        }

        // 採番実行
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Project project)
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            var employeeName = HttpContext.Session.GetString("EmployeeName");

            if (string.IsNullOrEmpty(employeeId))
            {
                return Json(new { error = "ログインが必要です" });
            }

            try
            {
                // デバッグログ: 送信された担当者ID
                Console.WriteLine($"[Debug] 送信されたStaffId: '{project.StaffId}'");
                Console.WriteLine($"[Debug] Project全体: Category={project.Category}, ProjectName={project.ProjectName}, ClientName={project.ClientName}, Budget={project.Budget}");

                // 担当者情報を取得
                var staff = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == project.StaffId);

                if (staff == null)
                {
                    // デバッグログ: データベースに存在する担当者ID一覧
                    var allEmployees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
                    Console.WriteLine($"[Debug] データベース内の有効な担当者: {string.Join(", ", allEmployees.Select(e => $"'{e.EmployeeId}'"))}");

                    return Json(new { error = $"担当者が見つかりません (送信されたID: '{project.StaffId}')" });
                }

                project.StaffName = staff.Name;

                // Board連携: 案件Noがある場合、Board APIから案件情報を取得
                int? boardProjectId = null;
                if (!string.IsNullOrEmpty(project.CaseNumber))
                {
                    try
                    {
                        var boardProject = await _boardApiService.GetProjectByNumberAsync(project.CaseNumber);
                        if (boardProject != null)
                        {
                            // Board APIから案件名とクライアント名を取得して設定
                            if (boardProject.name != null)
                            {
                                project.ProjectName = boardProject.name.ToString();
                            }

                            if (boardProject.client?.name != null)
                            {
                                project.ClientName = boardProject.client.name.ToString();
                            }

                            // 見積り金額を取得（複数のフィールド候補を試す）
                            var amountStr = boardProject.quotation_amount?.ToString() ??
                                          boardProject.estimate_amount?.ToString() ??
                                          boardProject.order_amount?.ToString() ??
                                          boardProject.sales_amount?.ToString() ??
                                          boardProject.contract_amount?.ToString() ??
                                          boardProject.estimated_amount?.ToString() ??
                                          boardProject.quote_amount?.ToString() ??
                                          boardProject.amount?.ToString() ??
                                          boardProject.total_amount?.ToString() ??
                                          boardProject.budget?.ToString() ??
                                          boardProject.price?.ToString();

                            Console.WriteLine($"[Debug] 見積り金額取得試行 - quotation_amount={boardProject.quotation_amount}, order_amount={boardProject.order_amount}, sales_amount={boardProject.sales_amount}, amount={boardProject.amount}, 最終取得値={amountStr}");

                            if (!string.IsNullOrEmpty(amountStr))
                            {
                                if (int.TryParse(amountStr, out int amountValue))
                                {
                                    project.Budget = amountValue;
                                }
                            }

                            // Board project IDを保存（後で管理番号を更新するため）
                            if (boardProject.id != null)
                            {
                                boardProjectId = (int)boardProject.id;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Board連携エラーはログに出力するが処理は続行
                        Console.WriteLine($"Board API取得エラー: {ex.Message}");
                    }
                }

                // プロジェクトを作成
                var createdProject = await _projectService.CreateProjectAsync(
                    project,
                    employeeId,
                    employeeName ?? "Unknown"
                );

                // メール送信（一時的に無効化）
                // await _emailService.SendNotificationEmailAsync(createdProject);

                // Board連携: 受注番号をboardに登録
                if (boardProjectId.HasValue)
                {
                    try
                    {
                        await _boardApiService.UpdateProjectManagementNumberAsync(
                            boardProjectId.Value,
                            createdProject.ProjectNumber
                        );
                    }
                    catch (Exception ex)
                    {
                        // Board連携エラーはログに出力
                        Console.WriteLine($"Board管理番号更新エラー: {ex.Message}");
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

        // Board連携テスト - 管理番号が空の案件一覧
        [HttpGet]
        public async Task<IActionResult> BoardTest()
        {
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Board APIから案件一覧を取得
                var boardProjects = await _boardApiService.SearchProjectsAsync(100, false);

                // 管理番号が空の案件のみフィルタリング
                var emptyManagementProjects = boardProjects
                    .Where(p => string.IsNullOrEmpty(p.management_number?.ToString()))
                    .Select(p => new
                    {
                        Id = p.id?.ToString(),
                        ProjectNo = p.project_no?.ToString(),
                        Name = p.name?.ToString(),
                        ClientName = p.client?.name?.ToString(),
                        ManagementNumber = p.management_number?.ToString(),
                        OrderStatus = p.order_status_name?.ToString(),
                        CreatedAt = p.created_at?.ToString(),
                        // 見積り金額フィールドの候補をすべて試す
                        Amount = p.estimate_amount?.ToString() ?? p.quotation_amount?.ToString() ?? p.estimated_amount?.ToString() ?? p.quote_amount?.ToString() ?? p.amount?.ToString() ?? p.budget?.ToString() ?? p.price?.ToString() ?? ""
                    })
                    .ToList();

                ViewBag.Projects = emptyManagementProjects;
                ViewBag.TotalCount = emptyManagementProjects.Count;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Board API エラー: {ex.Message}";
                ViewBag.Projects = new List<object>();
                return View();
            }
        }
    }
}
