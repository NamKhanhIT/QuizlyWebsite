using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuizlyWebsite.Controllers
{
    public class UserController : Controller
    {
        private readonly QuizlyDbContext _context;

        public UserController(QuizlyDbContext context)
        {
            _context = context;
        }

        // GET: /user/profile
        public async Task<IActionResult> Profile()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Index", "Home");

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var user = await _context.TbUsers.FindAsync(userId);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // GET: /user/results
        public async Task<IActionResult> Results()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Index", "Home");

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var results = await _context.TbExamResults
                .Where(r => r.UserId == userId)
                .Include(r => r.Exam)
                .OrderByDescending(r => r.FinishedAt)
                .ToListAsync();

            return View(results);
        }

        // GET: /user/membership
        public async Task<IActionResult> Membership()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Index", "Home");

            var plans = await _context.TbMembershipPlans.ToListAsync();
            return View(plans);
        }

        // GET: /user/purchase-history
        public async Task<IActionResult> PurchaseHistory()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Index", "Home");

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var purchases = await _context.TbUserPurchases
                .Where(p => p.UserId == userId)
                .Include(p => p.Exam)
                .OrderByDescending(p => p.PurchasedAt)
                .ToListAsync();

            return View(purchases);
        }
    }
}
