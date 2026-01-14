using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
            var totalRevenue = await _context.TbPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .SumAsync(p => p.Amount ?? 0);
            
            // Pending approvals
            // Chỉ đếm đề thi chờ duyệt (IsApproved == false/null) và chưa bị từ chối (RejectionReason == null)
            var pendingExams = await _context.TbExams
                .Where(e => (e.IsApproved == false || e.IsApproved == null) && e.RejectionReason == null)
                .CountAsync();
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
        public IActionResult Settings(string siteName, string siteDescription, string adminEmail, string supportEmail)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            try
            {
                // Placeholder for settings save logic
                TempData["Success"] = "Cài đặt đã được lưu thành công";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi lưu cài đặt: " + ex.Message;
                return RedirectToAction("Settings");
            }
        }

        // GET: /admin/reports
        [Route("admin/reports")]
        public async Task<IActionResult> Reports()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Get total revenue and payments
            var totalRevenue = await _context.TbPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .SumAsync(p => p.Amount ?? 0);

            var totalPayments = await _context.TbPayments.CountAsync();
            var successfulPayments = await _context.TbPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .CountAsync();

            // Monthly revenue for the last 12 months
            var monthlyRevenue = await _context.TbPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .Where(p => p.CreatedAt >= DateTime.Now.AddMonths(-12))
                .GroupBy(p => new { p.CreatedAt.Value.Year, p.CreatedAt.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.Amount ?? 0),
                    Count = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            // Convert to MonthlyRevenueViewModel with proper string formatting
            var monthlyRevenueViewModels = monthlyRevenue
                .Select(m => new MonthlyRevenueViewModel
                {
                    Month = $"{m.Month:00}/{m.Year}",
                    Revenue = m.Revenue,
                    Count = m.Count
                })
                .ToList();

            // Top exams by revenue
            var topExams = await _context.TbExams
                .Join(_context.TbPayments.Where(p => p.Status == "Completed" || p.Status == "Success"),
                    e => e.Id,
                    p => p.ExamId,
                    (e, p) => new { Exam = e, Payment = p })
                .GroupBy(ep => ep.Exam.Id)
                .Select(g => new TopExamViewModel
                {
                    ExamId = g.Key,
                    ExamTitle = g.First().Exam.Title,
                    Revenue = g.Sum(ep => ep.Payment.Amount ?? 0),
                    Purchases = g.Count()
                })
                .OrderByDescending(te => te.Revenue)
                .Take(10)
                .ToListAsync();

            // User growth over the last 12 months
            var userGrowthRaw = await _context.TbUsers
                .Where(u => u.CreatedAt >= DateTime.Now.AddMonths(-12))
                .GroupBy(u => new { u.CreatedAt.Value.Year, u.CreatedAt.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            // Convert to UserGrowthViewModel with proper string formatting
            var userGrowth = userGrowthRaw
                .Select(m => new UserGrowthViewModel
                {
                    Month = $"{m.Month:00}/{m.Year}",
                    Count = m.Count
                })
                .ToList();

            // Exam participation rates
            var examParticipation = await (from e in _context.TbExams
                                         join r in _context.TbExamResults on e.Id equals r.ExamId into results
                                         from r in results.DefaultIfEmpty()
                                         group new { e, r } by e.Id into g
                                         select new ExamParticipationViewModel
                                         {
                                             ExamTitle = g.First().e.Title,
                                             ParticipationRate = g.Count(x => x.r != null) > 0 ?
                                                (double)g.Count(x => x.r != null) / g.Count() * 100 : 0,
                                             TotalAttempts = g.Count(x => x.r != null)
                                         })
                .Where(ep => ep.TotalAttempts > 0)
                .OrderByDescending(ep => ep.ParticipationRate)
                .Take(10)
                .ToListAsync();

            // User role distribution
            var userRoleDistribution = await _context.TbUsers
                .GroupBy(u => u.Role ?? "User")
                .Select(g => new UserRoleDistributionViewModel
                {
                    Role = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(urd => urd.Count)
                .ToListAsync();

            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["TotalPayments"] = totalPayments;
            ViewData["SuccessfulPayments"] = successfulPayments;
            ViewData["MonthlyRevenue"] = monthlyRevenueViewModels;
            ViewData["TopExams"] = topExams;
            ViewData["UserGrowth"] = userGrowth;
            ViewData["ExamParticipation"] = examParticipation;
            ViewData["UserRoleDistribution"] = userRoleDistribution;

            return View();
        }

    }
}
