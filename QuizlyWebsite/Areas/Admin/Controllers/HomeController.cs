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

        // GET: /admin
        public async Task<IActionResult> Index()
        {
            var totalExams = await _context.TbExams.CountAsync();
            var totalUsers = await _context.TbUsers.CountAsync();
            var totalQuestions = await _context.TbQuestions.CountAsync();
            var totalResults = await _context.TbExamResults.CountAsync();
            var totalRevenue = await _context.TbPayments.Where(p => p.Status == "Completed").SumAsync(p => p.Amount);

            ViewData["TotalExams"] = totalExams;
            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalQuestions"] = totalQuestions;
            ViewData["TotalResults"] = totalResults;
            ViewData["TotalRevenue"] = totalRevenue;

            return View();
        }

        // Exams Management
        public async Task<IActionResult> Exams(int page = 1)
        {
            var exams = await _context.TbExams
                .Include(e => e.Subject)
                .ThenInclude(s => s.Category)
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbExams.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View(exams);
        }

        // Create/Edit Exam
        public async Task<IActionResult> ExamForm(int? id)
        {
            var categories = await _context.TbCategories.ToListAsync();
            var subjects = await _context.TbSubjects.ToListAsync();

            ViewData["Categories"] = categories;
            ViewData["Subjects"] = subjects;

            if (id.HasValue)
            {
                var exam = await _context.TbExams.FindAsync(id);
                if (exam == null) return NotFound();
                return View(exam);
            }

            return View(new TbExam());
        }

        // Questions Management
        public async Task<IActionResult> Questions(int examId, int page = 1)
        {
            var questions = await _context.TbQuestions
                .Where(q => q.ExamId == examId)
                .OrderBy(q => q.Id)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null) return NotFound();

            ViewData["Exam"] = exam;
            var total = await _context.TbQuestions.Where(q => q.ExamId == examId).CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View(questions);
        }

        // Users Management
        public async Task<IActionResult> Users(int page = 1)
        {
            var users = await _context.TbUsers
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbUsers.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View(users);
        }

        // Categories Management
        public async Task<IActionResult> Categories(int page = 1)
        {
            var categories = await _context.TbCategories
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbCategories.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View(categories);
        }

        // Payments & Revenue
        public async Task<IActionResult> Payments(int page = 1)
        {
            var payments = await _context.TbPayments
                .Include(p => p.User)
                .Include(p => p.Exam)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbPayments.CountAsync();
            var totalRevenue = await _context.TbPayments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);

            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;
            ViewData["TotalRevenue"] = totalRevenue;

            return View(payments);
        }

        // Reports
        public async Task<IActionResult> Reports()
        {
            var examResults = await _context.TbExamResults
                .Include(r => r.Exam)
                .GroupBy(r => r.Exam.Title)
                .Select(g => new { Exam = g.Key, Count = g.Count(), AvgScore = g.Average(r => r.Score) })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            ViewData["ExamResults"] = examResults;

            return View();
        }
    }
}
