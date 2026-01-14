using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // GET: Admin/Payments
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var quizlyDbContext = _context.TbPayments.Include(t => t.Exam).Include(t => t.User);
            return View(await quizlyDbContext.OrderByDescending(p => p.CreatedAt).ToListAsync());
        }

        // GET: Admin/Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbPayment = await _context.TbPayments
                .Include(t => t.Exam)
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbPayment == null)
            {
                return NotFound();
            }

            return View(tbPayment);
        }

        // GET: Admin/Payments/Delete/5
        public IActionResult Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            TempData["Error"] = "Không được phép xóa giao dịch. Chức năng này đã bị vô hiệu hóa vì lý do bảo mật.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            TempData["Error"] = "Không được phép xóa giao dịch. Chức năng này đã bị vô hiệu hóa vì lý do bảo mật.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Payments/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Unauthorized" });

            var payment = await _context.TbPayments.FindAsync(id);
            if (payment == null)
            {
                return Json(new { success = false, message = "Payment not found" });
            }

            // Validate status
            var validStatuses = new[] { "Pending", "Completed", "Failed" };
            if (!validStatuses.Contains(status))
            {
                return Json(new { success = false, message = "Invalid status" });
            }

            payment.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Status updated successfully" });
        }

        private bool TbPaymentExists(int id)
        {
            return _context.TbPayments.Any(e => e.Id == id);
        }
    }
}
