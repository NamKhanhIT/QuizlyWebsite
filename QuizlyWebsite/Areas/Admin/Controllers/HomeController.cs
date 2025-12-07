using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly QuizlyDbContext _context;

        public HomeController(QuizlyDbContext context)
        {
            _context = context;
        }

        // Check admin authorization
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var totalExams = await _context.TbExams.CountAsync();
            var totalUsers = await _context.TbUsers.CountAsync();
            var totalQuestions = await _context.TbQuestions.CountAsync();
            var totalResults = await _context.TbExamResults.CountAsync();
            var totalRevenue = await _context.TbPayments.Where(p => p.Status == "Completed").SumAsync(p => p.Amount);
            
            // Pending approvals
            var pendingExams = await _context.TbExams.Where(e => e.IsApproved == false).CountAsync();
            var pendingCourses = await _context.TbCourses.Where(c => c.IsApproved == false).CountAsync();
            var pendingLessons = await _context.TbLessons.Where(l => l.IsApproved == false).CountAsync();

            ViewData["TotalExams"] = totalExams;
            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalQuestions"] = totalQuestions;
            ViewData["TotalResults"] = totalResults;
            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["PendingExams"] = pendingExams;
            ViewData["PendingCourses"] = pendingCourses;
            ViewData["PendingLessons"] = pendingLessons;

            return View();
        }

        // Settings
        public IActionResult Settings()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Settings(string siteName, string siteDescription, string adminEmail, string supportEmail)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Placeholder for settings save logic
            TempData["Success"] = "Cài đặt đã được lưu thành công";
            return RedirectToAction("Settings");
        }
    }
}
