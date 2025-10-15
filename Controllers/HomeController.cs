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
    }
}
