using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExamReviewController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<ExamReviewController> _logger;

        public ExamReviewController(QuizlyDbContext context, ILogger<ExamReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/ExamReview
        public async Task<IActionResult> Index(string? filterStatus = "pending")
        {
            // Check if user is admin
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var user = await _context.TbUsers.FindAsync(userId);
            if (user?.Role != "Admin")
                return Forbid();

            IQueryable<TbExam> query = _context.TbExams
                .Include(e => e.Subject)
                .Include(e => e.CreatedByNavigation)
                .Include(e => e.ApprovedByNavigation);

            // Filter by approval status
            if (filterStatus == "approved")
                query = query.Where(e => e.IsApproved == true);
            else if (filterStatus == "rejected")
                query = query.Where(e => e.IsApproved == false && !string.IsNullOrEmpty(e.RejectionReason));
            else // pending
                query = query.Where(e => e.IsApproved == false && string.IsNullOrEmpty(e.RejectionReason));

            var exams = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewData["FilterStatus"] = filterStatus;
            ViewData["TotalPending"] = await _context.TbExams
                .Where(e => e.IsApproved == false && string.IsNullOrEmpty(e.RejectionReason))
                .CountAsync();

            return View(exams);
        }

        // GET: Admin/ExamReview/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.TbUsers.FindAsync(userId);
            if (user?.Role != "Admin")
                return Forbid();

            var exam = await _context.TbExams
                .Include(e => e.Subject)
                .Include(e => e.CreatedByNavigation)
                .Include(e => e.ApprovedByNavigation)
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
                return NotFound();

            return View(exam);
        }

        // POST: Admin/ExamReview/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.TbUsers.FindAsync(userId);
            if (user?.Role != "Admin")
                return Forbid();

            var exam = await _context.TbExams.FindAsync(id);
            if (exam == null)
                return NotFound();

            try
            {
                exam.IsApproved = true;
                exam.ApprovedBy = userId.Value;
                exam.ApprovedAt = DateTime.Now;
                exam.RejectionReason = null;

                _context.Update(exam);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin {userId} approved exam {id}");
                TempData["SuccessMessage"] = "Exam approved successfully!";
                return RedirectToAction("Index", "ExamReview");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving exam: {ex.Message}");
                TempData["ErrorMessage"] = "Error approving exam";
                return RedirectToAction("Detail", "ExamReview", new { id });
            }
        }

        // POST: Admin/ExamReview/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, [FromForm] string reason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.TbUsers.FindAsync(userId);
            if (user?.Role != "Admin")
                return Forbid();

            var exam = await _context.TbExams.FindAsync(id);
            if (exam == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Please provide a rejection reason";
                return RedirectToAction("Detail", "ExamReview", new { id });
            }

            try
            {
                exam.IsApproved = false;
                exam.ApprovedBy = null;
                exam.ApprovedAt = null;
                exam.RejectionReason = reason.Trim();

                _context.Update(exam);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin {userId} rejected exam {id}");
                TempData["SuccessMessage"] = "Exam rejected successfully!";
                return RedirectToAction("Index", "ExamReview");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting exam: {ex.Message}");
                TempData["ErrorMessage"] = "Error rejecting exam";
                return RedirectToAction("Detail", "ExamReview", new { id });
            }
        }
    }
}
