using Microsoft.AspNetCore.Mvc;
using ProjectOrderNumberSystem.Data;

namespace ProjectOrderNumberSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // セッションチェック
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["EmployeeId"] = employeeId;
            ViewData["EmployeeName"] = HttpContext.Session.GetString("EmployeeName");

            return View();
        }

        /// <summary>
        /// ヘルスチェック用エンドポイント（IP制限除外）
        /// 外部監視サービスからの定期アクセス用
        /// </summary>
        [HttpGet]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "ProjectOrderNumberSystem"
            });
        }
    }
}
