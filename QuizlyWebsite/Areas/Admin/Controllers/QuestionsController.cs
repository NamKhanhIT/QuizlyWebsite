using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuestionsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public QuestionsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/questions?examId={id}
        [Route("admin/questions")]
        public async Task<IActionResult> Index(int examId = 0, int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (examId > 0)
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

                return View("~/Areas/Admin/Views/Home/Questions.cshtml", questions);
            }
            else
            {
                // Show exams to select from
                var exams = await _context.TbExams
                    .Include(e => e.Subject)
                    .OrderBy(e => e.Id)
                    .ToListAsync();
                
                ViewData["Exams"] = exams;
                return View("~/Areas/Admin/Views/Home/Questions.cshtml", new List<TbQuestion>());
            }
        }

        // GET: /admin/question-form or /admin/question-form/{id}?examId={examId}
        [Route("admin/question-form")]
        [Route("admin/question-form/{id}")]
        public async Task<IActionResult> Form(int? id, int examId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null) return NotFound();

            ViewData["ExamId"] = examId;

            if (id.HasValue)
            {
                var question = await _context.TbQuestions.FindAsync(id.Value);
                if (question == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", question);
            }

            return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", new TbQuestion { ExamId = examId });
        }

        // POST: /admin/question-form or /admin/question-form/{id}?examId={examId}
        [HttpPost]
        [Route("admin/question-form")]
        [Route("admin/question-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, int examId, string content, string optionA, string optionB, string optionC, string optionD, string correctOption, decimal marks)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (string.IsNullOrWhiteSpace(content))
                ModelState.AddModelError(nameof(content), "Nội dung câu hỏi không được để trống");

            if (!ModelState.IsValid)
            {
                ViewData["ExamId"] = examId;
                var model = id.HasValue ? await _context.TbQuestions.FindAsync(id) : new TbQuestion { ExamId = examId };
                return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", model);
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var question = await _context.TbQuestions.FindAsync(id.Value);
                    if (question == null) return NotFound();
                    question.Content = content;
                    question.OptionA = optionA;
                    question.OptionB = optionB;
                    question.OptionC = optionC;
                    question.OptionD = optionD;
                    question.CorrectOption = correctOption;
                    question.Marks = marks;

                    _context.TbQuestions.Update(question);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Câu hỏi đã được cập nhật";
                }
                else
                {
                    var question = new TbQuestion
                    {
                        ExamId = examId,
                        Content = content,
                        OptionA = optionA,
                        OptionB = optionB,
                        OptionC = optionC,
                        OptionD = optionD,
                        CorrectOption = correctOption,
                        Marks = marks
                    };

                    await _context.TbQuestions.AddAsync(question);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Câu hỏi mới đã được tạo";
                }

                return RedirectToAction("Index", new { examId = examId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                ViewData["ExamId"] = examId;
                var model = id.HasValue ? await _context.TbQuestions.FindAsync(id) : new TbQuestion { ExamId = examId };
                return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", model);
            }
        }

        // POST: /admin/delete-question (used by AJAX)
        [HttpPost]
        [Route("admin/delete-question")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var question = await _context.TbQuestions.FindAsync(req.Id);
            if (question == null) return Json(new { success = false, message = "Câu hỏi không tồn tại" });

            _context.TbQuestions.Remove(question);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Câu hỏi đã được xóa" });
        }

        public class DeleteRequest { public int Id { get; set; } }
    }
}
