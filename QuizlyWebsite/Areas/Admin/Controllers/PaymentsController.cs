using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PaymentsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public PaymentsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/payments
        [Route("admin/payments")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var payments = await _context.TbPayments
                .Include(p => p.User)
                .Include(p => p.Exam)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbPayments.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/Payments.cshtml", payments);
        }

        // GET: /admin/reports
        [Route("admin/reports")]
        public async Task<IActionResult> Reports()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Get all payments first, then process in-memory
            var allPayments = await _context.TbPayments
                .Include(p => p.User)
                .Include(p => p.Exam)
                .ToListAsync();

            // Revenue stats
            var totalRevenue = allPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .Sum(p => p.Amount ?? 0);

            // Monthly revenue (calculated in-memory)
            var monthlyRevenue = allPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success" && p.CreatedAt.HasValue)
                .GroupBy(p => new { Year = p.CreatedAt!.Value.Year, Month = p.CreatedAt.Value.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Revenue = g.Sum(p => p.Amount ?? 0),
                    Count = g.Count()
                })
                .OrderBy(r => r.Month)
                .ToList();

            // Payment method stats (using Provider instead)
            var paymentMethodStats = allPayments
                .GroupBy(p => p.Provider ?? "Unknown")
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(p => p.Amount ?? 0)
                })
                .ToList();

            // Top exams by revenue
            var topExams = allPayments
                .Where(p => (p.Status == "Completed" || p.Status == "Success") && p.ExamId.HasValue)
                .GroupBy(p => new { p.ExamId, p.Exam!.Title })
                .Select(g => new
                {
                    ExamId = g.Key.ExamId,
                    ExamTitle = g.Key.Title,
                    Revenue = g.Sum(p => p.Amount ?? 0),
                    Purchases = g.Count()
                })
                .OrderByDescending(e => e.Revenue)
                .Take(5)
                .ToList();

            // User growth (calculated in-memory)
            var userGrowth = await _context.TbUsers
                .Where(u => u.CreatedAt.HasValue)
                .ToListAsync();

            var userGrowthList = userGrowth
                .GroupBy(u => new { Year = u.CreatedAt!.Value.Year, Month = u.CreatedAt.Value.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Count = g.Count()
                })
                .OrderBy(u => u.Month)
                .ToList();

            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["MonthlyRevenue"] = monthlyRevenue;
            ViewData["PaymentMethodStats"] = paymentMethodStats;
            ViewData["TopExams"] = topExams;
            ViewData["UserGrowth"] = userGrowthList;
            ViewData["TotalPayments"] = allPayments.Count;
            ViewData["SuccessfulPayments"] = allPayments.Count(p => p.Status == "Completed" || p.Status == "Success");

            return View("~/Areas/Admin/Views/Home/Reports.cshtml");
        }

        // POST: /admin/update-payment-status (used by AJAX)
        [HttpPost]
        [Route("admin/update-payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus([FromBody] UpdatePaymentStatusRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var payment = await _context.TbPayments.FindAsync(req.Id);
            if (payment == null) return Json(new { success = false, message = "Giao dịch không tồn tại" });

            payment.Status = req.Status; // "Completed", "Failed", etc.
            _context.TbPayments.Update(payment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Trạng thái giao dịch đã được cập nhật" });
        }

        // POST: /admin/delete-payment (used by AJAX)
        [HttpPost]
        [Route("admin/delete-payment")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var payment = await _context.TbPayments.FindAsync(req.Id);
            if (payment == null) return Json(new { success = false, message = "Giao dịch không tồn tại" });

            _context.TbPayments.Remove(payment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Giao dịch đã được xóa" });
        }

        public class DeleteRequest { public int Id { get; set; } }
        public class UpdatePaymentStatusRequest { public int Id { get; set; } public string Status { get; set; } }
    }
}
