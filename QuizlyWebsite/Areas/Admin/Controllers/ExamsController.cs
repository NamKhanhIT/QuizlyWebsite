using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;


namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExamsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public ExamsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/exams
        [Route("admin/exams")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var exams = await _context.TbExams
                .Include(e => e.Subject)
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbExams.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/Exams.cshtml", exams);
        }

        // GET: /admin/exam-form or /admin/exam-form/{id}
        [Route("admin/exam-form")]
        [Route("admin/exam-form/{id}")]
        public async Task<IActionResult> Form(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var subjects = await _context.TbSubjects.ToListAsync();
            var categories = await _context.TbCategories.ToListAsync();

            ViewData["Subjects"] = subjects;
            ViewData["Categories"] = categories;

            if (id.HasValue)
            {
                var exam = await _context.TbExams.FindAsync(id.Value);
                if (exam == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", exam);
            }

            return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", new TbExam());
        }

        // POST: /admin/exam-form or /admin/exam-form/{id}
        [HttpPost]
        [Route("admin/exam-form")]
        [Route("admin/exam-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, string title, int subjectId, int questionCount, int duration, string difficulty, bool isPremium, decimal? price)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError(nameof(title), "Tên đề thi không được để trống");

            if (!ModelState.IsValid)
            {
                var subjects = await _context.TbSubjects.ToListAsync();
                var categories = await _context.TbCategories.ToListAsync();
                ViewData["Subjects"] = subjects;
                ViewData["Categories"] = categories;
                var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var exam = await _context.TbExams.FindAsync(id.Value);
                    if (exam == null) return NotFound();

                    exam.Title = title;
                    exam.SubjectId = subjectId;
                    exam.QuestionCount = questionCount;
                    exam.Duration = duration;
                    exam.Difficulty = difficulty;
                    exam.IsPremium = isPremium;
                    exam.Price = isPremium ? (price ?? 0) : 0; // sửa lại

                    _context.TbExams.Update(exam);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đề thi đã được cập nhật";
                }
                else
                {
                    var exam = new TbExam
                    {
                        Title = title,
                        SubjectId = subjectId,
                        QuestionCount = questionCount,
                        Duration = duration,
                        Difficulty = difficulty,
                        IsPremium = isPremium,
                        Price = isPremium ? (price ?? 0) : 0, // sửa lại
                        CreatedAt = DateTime.Now
                    };

                    await _context.TbExams.AddAsync(exam);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đề thi mới đã được tạo";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                var subjects = await _context.TbSubjects.ToListAsync();
                var categories = await _context.TbCategories.ToListAsync();
                ViewData["Subjects"] = subjects;
                ViewData["Categories"] = categories;
                var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
            }
        }

        [HttpPost]
        [Route("admin/delete-exam")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest req) //kiểm tra quyền được xóa hay không
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var exam = await _context.TbExams.FindAsync(req.Id);
            if (exam == null) return Json(new { success = false, message = "Đề thi không tồn tại" });

            _context.TbExams.Remove(exam);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đề thi đã được xóa" });
        }

        public class DeleteRequest { public int Id { get; set; } } // lấy id người dùng 
    }
}
