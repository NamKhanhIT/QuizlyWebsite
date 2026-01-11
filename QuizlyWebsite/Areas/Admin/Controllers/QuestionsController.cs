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

        // Method to sync QuestionCount for all exams (for data consistency)
        private async Task SyncQuestionCountsAsync()
        {
            var allExams = await _context.TbExams.ToListAsync();
            foreach (var exam in allExams)
            {
                var questionCount = await _context.TbQuestions.CountAsync(q => q.ExamId == exam.Id);
                if (exam.QuestionCount != questionCount)
                {
                    exam.QuestionCount = questionCount;
                }
            }
            await _context.SaveChangesAsync();
        }

        // GET: Admin/Questions
        public async Task<IActionResult> Index(int? examId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Sync question counts to ensure data consistency
            await SyncQuestionCountsAsync();

            var query = _context.TbQuestions.Include(t => t.Exam).AsQueryable();

            if (examId.HasValue)
            {
                query = query.Where(q => q.ExamId == examId.Value);
                ViewData["ExamId"] = examId.Value;
                var exam = await _context.TbExams.FindAsync(examId.Value);
                ViewData["Exam"] = exam;
            }

            return View(await query.OrderBy(q => q.Id).ToListAsync());
        }

        // GET: Admin/Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbQuestion = await _context.TbQuestions
                .Include(t => t.Exam)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbQuestion == null)
            {
                return NotFound();
            }

            return View(tbQuestion);
        }

        // GET: Admin/Questions/Create
        public IActionResult Create(int? examId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewData["ExamId"] = new SelectList(_context.TbExams, "Id", "Title", examId);
            if (examId.HasValue)
            {
                ViewData["ExamId"] = new SelectList(_context.TbExams.Where(e => e.Id == examId.Value), "Id", "Title", examId);
            }
            return View();
        }

        // POST: Admin/Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ExamId,Content,OptionA,OptionB,OptionC,OptionD,CorrectOption,Marks")] TbQuestion tbQuestion)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (ModelState.IsValid)
            {
                // Check question count constraint
                var currentExam = await _context.TbExams
                    .Include(e => e.TbQuestions)
                    .FirstOrDefaultAsync(e => e.Id == tbQuestion.ExamId);

                if (currentExam == null)
                {
                    ModelState.AddModelError("", "Đề thi không tồn tại");
                    ViewData["ExamId"] = new SelectList(_context.TbExams, "Id", "Title", tbQuestion.ExamId);
                    return View(tbQuestion);
                }

                int currentQuestionCount = currentExam.TbQuestions.Count;
                int requiredQuestionCount = currentExam.QuestionCount ?? 0;

                if (currentQuestionCount >= requiredQuestionCount)
                {
                    ModelState.AddModelError("", $"Đề thi này đã có đủ {requiredQuestionCount} câu hỏi. Không thể thêm câu hỏi mới.");
                    ViewData["ExamId"] = new SelectList(_context.TbExams, "Id", "Title", tbQuestion.ExamId);
                    return View(tbQuestion);
                }

                // Add the new question first
                _context.Add(tbQuestion);
                await _context.SaveChangesAsync();

                // Redistribute marks for ALL questions in the exam (total score = 10 points)
                var allQuestionsInExam = await _context.TbQuestions
                    .Where(q => q.ExamId == tbQuestion.ExamId)
                    .ToListAsync();

                decimal totalScore = 10; // Total exam score is always 10 points
                decimal marksPerQuestion = totalScore / allQuestionsInExam.Count;

                foreach (var question in allQuestionsInExam)
                {
                    question.Marks = marksPerQuestion;
                }

                await _context.SaveChangesAsync();

                // Update QuestionCount in exam
                var examToUpdate = await _context.TbExams.FindAsync(tbQuestion.ExamId);
                if (examToUpdate != null)
                {
                    examToUpdate.QuestionCount = allQuestionsInExam.Count;
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Câu hỏi đã được tạo thành công và điểm số đã được phân bổ lại";
                return RedirectToAction(nameof(Index), new { examId = tbQuestion.ExamId });
            }
            ViewData["ExamId"] = new SelectList(_context.TbExams, "Id", "Title", tbQuestion.ExamId);
            return View(tbQuestion);
        }

        // GET: Admin/Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbQuestion = await _context.TbQuestions.FindAsync(id);
            if (tbQuestion == null)
            {
                return NotFound();
            }
            ViewData["ExamId"] = new SelectList(_context.TbExams, "Id", "Title", tbQuestion.ExamId);
            return View(tbQuestion);
        }

        // POST: Admin/Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ExamId,Content,OptionA,OptionB,OptionC,OptionD,CorrectOption,Marks")] TbQuestion tbQuestion)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbQuestion.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbQuestion);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Câu hỏi đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbQuestionExists(tbQuestion.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { examId = tbQuestion.ExamId });
            }
            ViewData["ExamId"] = new SelectList(_context.TbExams, "Id", "Title", tbQuestion.ExamId);
            return View(tbQuestion);
        }

        // GET: Admin/Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbQuestion = await _context.TbQuestions
                .Include(t => t.Exam)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbQuestion == null)
            {
                return NotFound();
            }

            return View(tbQuestion);
        }

        // POST: Admin/Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbQuestion = await _context.TbQuestions.FindAsync(id);
            if (tbQuestion != null)
            {
                var examId = tbQuestion.ExamId;

                // Check question count constraint - allow deletion if there will be at least 1 question left
                var currentExam = await _context.TbExams
                    .Include(e => e.TbQuestions)
                    .FirstOrDefaultAsync(e => e.Id == examId);

                if (currentExam != null)
                {
                    int currentQuestionCount = currentExam.TbQuestions.Count;

                    // Don't allow deletion if it would leave 0 questions
                    if (currentQuestionCount <= 1)
                    {
                        TempData["Error"] = "Đề thi phải có ít nhất 1 câu hỏi. Không thể xóa câu hỏi cuối cùng.";
                        return RedirectToAction(nameof(Index), new { examId = examId });
                    }
                }

                // Delete related exam result details first
                var relatedResultDetails = await _context.TbExamResultDetails
                    .Where(d => d.QuestionId == tbQuestion.Id)
                    .ToListAsync();
                _context.TbExamResultDetails.RemoveRange(relatedResultDetails);
                await _context.SaveChangesAsync();

                _context.TbQuestions.Remove(tbQuestion);
                await _context.SaveChangesAsync();

                // Redistribute points among remaining questions (total score = 10 points)
                var remainingQuestions = await _context.TbQuestions
                    .Where(q => q.ExamId == examId)
                    .ToListAsync();

                if (remainingQuestions.Any())
                {
                    decimal totalScore = 10; // Total exam score is always 10 points
                    decimal pointsPerQuestion = totalScore / remainingQuestions.Count;

                    foreach (var question in remainingQuestions)
                    {
                        question.Marks = pointsPerQuestion;
                    }

                    await _context.SaveChangesAsync();

                    // Update QuestionCount in exam
                    var examToUpdate = await _context.TbExams.FindAsync(examId);
                    if (examToUpdate != null)
                    {
                        examToUpdate.QuestionCount = remainingQuestions.Count;
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // If no questions left, set QuestionCount to 0
                    var examToUpdate = await _context.TbExams.FindAsync(examId);
                    if (examToUpdate != null)
                    {
                        examToUpdate.QuestionCount = 0;
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = "Câu hỏi đã được xóa thành công và điểm số đã được phân bổ lại";
            }

            return RedirectToAction(nameof(Index), new { examId = tbQuestion?.ExamId });
        }

        private bool TbQuestionExists(int id)
        {
            return _context.TbQuestions.Any(e => e.Id == id);
        }
    }
}
