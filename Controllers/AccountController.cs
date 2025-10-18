using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrderNumberSystem.Data;
using ProjectOrderNumberSystem.Models;

namespace ProjectOrderNumberSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string employeeId, string password)
        {
            try
            {
                // データベース接続テスト
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    TempData["Error"] = "データベースに接続できません";
                    Console.WriteLine("[ERROR] Database connection failed");
                    return View();
                }

                Console.WriteLine($"[INFO] Login attempt for employee: {employeeId}");

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                {
                    Console.WriteLine($"[WARNING] Employee not found: {employeeId}");
                    TempData["Error"] = "社員番号が見つかりません。新規アカウント作成をしてください。";
                    return View();
                }

                // 簡易認証: 社員番号=パスワード
                if (employeeId == password)
                {
                    // セッションに保存
                    HttpContext.Session.SetString("EmployeeId", employee.EmployeeId);
                    HttpContext.Session.SetString("EmployeeName", employee.Name);
                    HttpContext.Session.SetString("Role", employee.Role);

                    Console.WriteLine($"[INFO] Login successful: {employeeId}");
                    TempData["Success"] = "ログインしました";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    Console.WriteLine($"[WARNING] Password mismatch for: {employeeId}");
                    TempData["Error"] = "パスワードが正しくありません";
                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Login exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                TempData["Error"] = $"ログイン中にエラーが発生しました: {ex.Message}";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "ログアウトしました";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            // 管理者権限チェック
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                TempData["Error"] = "管理者権限が必要です";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string employeeId, string name, string? email)
        {
            // 管理者権限チェック
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                TempData["Error"] = "管理者権限が必要です";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(name))
            {
                TempData["Error"] = "必須項目を入力してください";
                return View();
            }

            // 既に登録済みかチェック
            var existing = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (existing != null)
            {
                TempData["Error"] = "この社員番号は既に登録されています";
                return View();
            }

            // 新規社員を作成
            var newEmployee = new Employee
            {
                EmployeeId = employeeId,
                Name = name,
                Email = email,
                IsActive = true,
                Role = "user"
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"アカウントを作成しました。社員番号: {employeeId}、初期パスワード: {employeeId}";
            return RedirectToAction("UserList");
        }

        /// <summary>
        /// ユーザー一覧画面（管理者のみ）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UserList()
        {
            // 管理者権限チェック
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                TempData["Error"] = "管理者権限が必要です";
                return RedirectToAction("Index", "Home");
            }

            // 全ユーザーを取得（社員番号順）
            var users = await _context.Employees
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            return View(users);
        }

        /// <summary>
        /// ユーザー権限変更（管理者のみ）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangeRole(string employeeId, string newRole)
        {
            // 管理者権限チェック
            var currentRole = HttpContext.Session.GetString("Role");
            if (currentRole != "admin")
            {
                TempData["Error"] = "管理者権限が必要です";
                return RedirectToAction("Index", "Home");
            }

            // 自分自身の権限は変更できない
            var currentEmployeeId = HttpContext.Session.GetString("EmployeeId");
            if (employeeId == currentEmployeeId)
            {
                TempData["Error"] = "自分自身の権限は変更できません";
                return RedirectToAction("UserList");
            }

            // 権限の妥当性チェック
            if (newRole != "admin" && newRole != "user")
            {
                TempData["Error"] = "無効な権限です";
                return RedirectToAction("UserList");
            }

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                {
                    TempData["Error"] = "ユーザーが見つかりません";
                    return RedirectToAction("UserList");
                }

                var oldRole = employee.Role;
                employee.Role = newRole;
                await _context.SaveChangesAsync();

                var roleName = newRole == "admin" ? "管理者" : "一般ユーザー";
                TempData["Success"] = $"{employee.Name}（{employeeId}）の権限を「{roleName}」に変更しました";
                Console.WriteLine($"[INFO] Role changed: {employeeId} ({oldRole} → {newRole})");

                return RedirectToAction("UserList");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"権限変更に失敗しました: {ex.Message}";
                Console.WriteLine($"[ERROR] Role change failed: {ex.Message}");
                return RedirectToAction("UserList");
            }
        }

        /// <summary>
        /// ユーザー有効/無効切り替え（管理者のみ）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleActive(string employeeId)
        {
            // 管理者権限チェック
            var currentRole = HttpContext.Session.GetString("Role");
            if (currentRole != "admin")
            {
                TempData["Error"] = "管理者権限が必要です";
                return RedirectToAction("Index", "Home");
            }

            // 自分自身は無効化できない
            var currentEmployeeId = HttpContext.Session.GetString("EmployeeId");
            if (employeeId == currentEmployeeId)
            {
                TempData["Error"] = "自分自身を無効化することはできません";
                return RedirectToAction("UserList");
            }

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                {
                    TempData["Error"] = "ユーザーが見つかりません";
                    return RedirectToAction("UserList");
                }

                employee.IsActive = !employee.IsActive;
                await _context.SaveChangesAsync();

                var status = employee.IsActive ? "有効" : "無効";
                TempData["Success"] = $"{employee.Name}（{employeeId}）を「{status}」に変更しました";
                Console.WriteLine($"[INFO] Active status changed: {employeeId} → {status}");

                return RedirectToAction("UserList");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"状態変更に失敗しました: {ex.Message}";
                Console.WriteLine($"[ERROR] Active toggle failed: {ex.Message}");
                return RedirectToAction("UserList");
            }
        }

        /// <summary>
        /// ユーザー編集画面表示（管理者のみ）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditUser(string employeeId)
        {
            // 管理者権限チェック
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                TempData["Error"] = "管理者権限が必要です";
                return RedirectToAction("Index", "Home");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                TempData["Error"] = "ユーザーが見つかりません";
                return RedirectToAction("UserList");
            }

            return View(employee);
        }

        /// <summary>
        /// ユーザー情報更新（管理者のみ）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> EditUser(string employeeId, string name, string? email)
        {
            // 管理者権限チェック
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                TempData["Error"] = "管理者権限が必要です";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(name))
            {
                TempData["Error"] = "氏名は必須です";
                return RedirectToAction("EditUser", new { employeeId });
            }

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                {
                    TempData["Error"] = "ユーザーが見つかりません";
                    return RedirectToAction("UserList");
                }

                employee.Name = name;
                employee.Email = email;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"{employee.Name}（{employeeId}）の情報を更新しました";
                Console.WriteLine($"[INFO] User info updated: {employeeId}");

                // 自分自身を編集した場合、セッション情報も更新
                var currentEmployeeId = HttpContext.Session.GetString("EmployeeId");
                if (employeeId == currentEmployeeId)
                {
                    HttpContext.Session.SetString("EmployeeName", name);
                }

                return RedirectToAction("UserList");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"更新に失敗しました: {ex.Message}";
                Console.WriteLine($"[ERROR] User update failed: {ex.Message}");
                return RedirectToAction("EditUser", new { employeeId });
            }
        }
    }
}
