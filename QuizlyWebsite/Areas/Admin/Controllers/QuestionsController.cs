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

        // GET: Admin/Questions
        public async Task<IActionResult> Index(int? examId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

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

            if (!examId.HasValue)
            {
                return BadRequest("ExamId is required");
            }

            var exam = _context.TbExams.FirstOrDefault(e => e.Id == examId.Value);
            if (exam == null)
            {
                return NotFound("Đề thi không tồn tại");
            }

            var model = new TbQuestion
            {
                ExamId = examId.Value
            };

            ViewBag.ExamTitle = exam.Title;

            return View(model);
        }

        // POST: Admin/Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ExamId,Content,OptionA,OptionB,OptionC,OptionD,CorrectOption")] TbQuestion tbQuestion)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Remove validation errors for navigation property Exam
            ModelState.Remove("Exam");

            // Validate CorrectOption is required
            if (string.IsNullOrWhiteSpace(tbQuestion.CorrectOption))
            {
                ModelState.AddModelError("CorrectOption", "Vui lòng chọn đáp án đúng");
            }
            else if (!new[] { "A", "B", "C", "D" }.Contains(tbQuestion.CorrectOption.ToUpperInvariant()))
            {
                ModelState.AddModelError("CorrectOption", "Đáp án đúng phải là A, B, C hoặc D");
            }
            else
            {
                // Normalize to uppercase
                tbQuestion.CorrectOption = tbQuestion.CorrectOption.ToUpperInvariant();
            }

            if (ModelState.IsValid)
            {
                // Check question count constraint
                var currentExam = await _context.TbExams
                    .Include(e => e.TbQuestions)
                    .FirstOrDefaultAsync(e => e.Id == tbQuestion.ExamId);

                if (currentExam == null)
                {
                    ModelState.AddModelError("", "Đề thi không tồn tại");
                    ViewBag.ExamTitle = "Đề thi #" + tbQuestion.ExamId;
                    return View(tbQuestion);
                }

                int currentQuestionCount = currentExam.TbQuestions.Count;
                int requiredQuestionCount = currentExam.QuestionCount ?? 0;

                // Only block if there's a specific question count limit AND we've reached it
                // If requiredQuestionCount is 0, allow unlimited questions
                if (requiredQuestionCount > 0 && currentQuestionCount >= requiredQuestionCount)
                {
                    ModelState.AddModelError("", $"Đề thi này đã có đủ {requiredQuestionCount} câu hỏi. Không thể thêm câu hỏi mới.");
                    ViewBag.ExamTitle = currentExam.Title;
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

                TempData["Success"] = "Câu hỏi đã được tạo thành công và điểm số đã được phân bổ lại";
                return RedirectToAction(nameof(Index), new { examId = tbQuestion.ExamId });
            }

            // If validation fails, set ViewBag for display
            var examForView = await _context.TbExams.FindAsync(tbQuestion.ExamId);
            ViewBag.ExamTitle = examForView?.Title ?? "Đề thi #" + tbQuestion.ExamId;
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
                }

                TempData["Success"] = "Câu hỏi đã được xóa thành công và điểm số đã được phân bổ lại";
            }

            return RedirectToAction(nameof(Index), new { examId = tbQuestion?.ExamId });
        }

        // POST: Admin/Questions/ImportQuestions
        [HttpPost]
        [Route("Admin/Questions/ImportQuestions")]
        public async Task<IActionResult> ImportQuestions([FromBody] ImportQuestionsRequest request)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền truy cập" });

            if (request == null || request.ExamId == 0 || request.Questions == null || !request.Questions.Any())
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                var exam = await _context.TbExams
                    .Include(e => e.TbQuestions)
                    .FirstOrDefaultAsync(e => e.Id == request.ExamId);
                if (exam == null)
                {
                    return Json(new { success = false, message = "Đề thi không tồn tại" });
                }

                // Count current questions ONCE before import (same logic as Create POST)
                int currentQuestionCount = exam.TbQuestions.Count;
                int requiredQuestionCount = exam.QuestionCount ?? 0;

                // Validate and count valid questions to import
                var validQuestions = new List<ImportedQuestion>();
                foreach (var q in request.Questions)
                {
                    // Validate question data
                    if (string.IsNullOrWhiteSpace(q.question) ||
                        string.IsNullOrWhiteSpace(q.optionA) ||
                        string.IsNullOrWhiteSpace(q.optionB) ||
                        string.IsNullOrWhiteSpace(q.optionC) ||
                        string.IsNullOrWhiteSpace(q.optionD) ||
                        string.IsNullOrWhiteSpace(q.correctOption))
                    {
                        continue; // Skip invalid questions
                    }

                    validQuestions.Add(q);
                }

                // Check question count constraint (same logic as Create POST)
                // Only block if there's a specific question count limit AND we've reached it
                // If requiredQuestionCount is 0, allow unlimited questions
                if (requiredQuestionCount > 0 && currentQuestionCount >= requiredQuestionCount)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Đề thi này đã có đủ {requiredQuestionCount} câu hỏi. Không thể thêm câu hỏi mới." 
                    });
                }

                // Check if import would exceed limit (all-or-nothing validation)
                if (requiredQuestionCount > 0)
                {
                    int totalAfterImport = currentQuestionCount + validQuestions.Count;
                    if (totalAfterImport > requiredQuestionCount)
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Không thể import: Đề thi đã có {currentQuestionCount} câu hỏi, giới hạn là {requiredQuestionCount} câu. Bạn đang cố import {validQuestions.Count} câu hỏi (tổng sẽ là {totalAfterImport} câu, vượt quá giới hạn {requiredQuestionCount} câu)." 
                        });
                    }
                }

                // All-or-nothing: Import all valid questions
                int addedCount = 0;
                foreach (var q in validQuestions)
                {
                    var question = new TbQuestion
                    {
                        ExamId = request.ExamId,
                        Content = q.question.Trim(),
                        OptionA = q.optionA.Trim(),
                        OptionB = q.optionB.Trim(),
                        OptionC = q.optionC.Trim(),
                        OptionD = q.optionD.Trim(),
                        CorrectOption = q.correctOption.Trim().ToUpperInvariant(),
                        Marks = null // Will be normalized later
                    };

                    _context.TbQuestions.Add(question);
                    addedCount++;
                }

                await _context.SaveChangesAsync();

                // Normalize marks to ensure total = 10
                await NormalizeExamMarksAsync(request.ExamId);

                return Json(new { success = true, addedCount = addedCount, message = $"Đã thêm thành công {addedCount} câu hỏi" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Helper method to normalize exam marks so total = 10
        private async Task NormalizeExamMarksAsync(int examId)
        {
            var questions = await _context.TbQuestions
                .Where(q => q.ExamId == examId)
                .OrderBy(q => q.Id)
                .ToListAsync();

            if (questions == null || !questions.Any())
                return;

            int questionCount = questions.Count;

            // Calculate equal marks per question: 10 / questionCount
            // Round to 2 decimal places
            decimal baseMarks = Math.Round(10m / questionCount, 2, MidpointRounding.AwayFromZero);

            // Distribute base marks to all questions
            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].Marks = baseMarks;
            }

            // Adjust the last question to ensure total exactly equals 10
            // Calculate what the total would be with base marks
            decimal currentTotal = baseMarks * questionCount;
            decimal difference = 10m - currentTotal;

            // Add the difference to the last question to make total exactly 10
            if (questions.Count > 0)
            {
                decimal lastQuestionMarks = baseMarks + difference;
                // Round to 2 decimal places
                questions[questions.Count - 1].Marks = Math.Round(lastQuestionMarks, 2, MidpointRounding.AwayFromZero);
            }

            _context.TbQuestions.UpdateRange(questions);
            await _context.SaveChangesAsync();
        }

        private bool TbQuestionExists(int id)
        {
            return _context.TbQuestions.Any(e => e.Id == id);
        }
    }

    public class ImportQuestionsRequest
    {
        public int ExamId { get; set; }
        public List<ImportedQuestion> Questions { get; set; } = new();
    }

    public class ImportedQuestion
    {
        public string question { get; set; } = string.Empty;
        public string optionA { get; set; } = string.Empty;
        public string optionB { get; set; } = string.Empty;
        public string optionC { get; set; } = string.Empty;
        public string optionD { get; set; } = string.Empty;
        public string correctOption { get; set; } = string.Empty;
    }
}
