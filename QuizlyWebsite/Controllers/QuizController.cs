using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuizlyWebsite.Controllers
{
    public class QuizController : Controller
    {
        private readonly QuizlyDbContext _context;

        public QuizController(QuizlyDbContext context)
        {
            _context = context;
        }

        // GET: /quiz/list
        public async Task<IActionResult> List(int? categoryId, string? search, int page = 1)
        {
            var query = _context.TbExams.AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.Subject.CategoryId == categoryId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Title.Contains(search));
            }

            var exams = await query
                .Include(e => e.Subject)
                .ThenInclude(s => s.Category)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewData["Categories"] = await _context.TbCategories.ToListAsync();
            ViewData["Search"] = search;
            ViewData["SelectedCategory"] = categoryId;

            return View(exams);
        }

        // GET: /quiz/detail/{id}
        public async Task<IActionResult> Detail(int id)
        {
            var exam = await _context.TbExams
                .Include(e => e.Subject)
                .ThenInclude(s => s.Category)
                .Include(e => e.TbExamReviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
                return NotFound();

            return View(exam);
        }

        // GET: /quiz/start/{id}
        public async Task<IActionResult> Start(int id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Index", "Home");

            var exam = await _context.TbExams
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
                return NotFound();

            // Create exam session
            var session = new TbExamSession
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                ExamId = id,
                StartedAt = DateTime.Now,
                Status = "Ongoing"
            };

            _context.TbExamSessions.Add(session);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("SessionId", session.Id);

            return View(exam);
        }

        // POST: /quiz/submit
        [HttpPost]
        public async Task<IActionResult> Submit(int examId, Dictionary<string, string> answers)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var sessionId = HttpContext.Session.GetInt32("SessionId");
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var exam = await _context.TbExams
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
                return NotFound();

            // Calculate score
            int correctAnswers = 0;
            var resultDetails = new List<TbExamResultDetail>();

            foreach (var question in exam.TbQuestions)
            {
                bool isCorrect = false;
                if (answers.TryGetValue(question.Id.ToString(), out var selectedOption))
                {
                    isCorrect = selectedOption?.ToUpperInvariant() == question.CorrectOption?.ToUpperInvariant();
                    if (isCorrect) correctAnswers++;
                }

                resultDetails.Add(new TbExamResultDetail
                {
                    QuestionId = question.Id,
                    SelectedOption = answers.TryGetValue(question.Id.ToString(), out var opt) ? opt : null,
                    IsCorrect = isCorrect
                });
            }

            var score = (decimal)correctAnswers / exam.TbQuestions.Count * 100;

            var result = new TbExamResult
            {
                UserId = userId,
                ExamId = examId,
                SessionId = sessionId,
                Score = score,
                CorrectAnswers = correctAnswers,
                TotalQuestions = exam.TbQuestions.Count,
                StartedAt = DateTime.Now.AddMinutes(-5),
                FinishedAt = DateTime.Now,
                Status = score >= 50 ? "Passed" : "Failed"
            };

            _context.TbExamResults.Add(result);
            await _context.SaveChangesAsync();

            foreach (var detail in resultDetails)
            {
                detail.ExamResultId = result.Id;
                _context.TbExamResultDetails.Add(detail);
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("SessionId");

            return RedirectToAction("Result", new { resultId = result.Id });
        }
        // GET: /quiz/result/{resultId}
        public async Task<IActionResult> Result(int resultId)
        {
            var result = await _context.TbExamResults
                .Include(r => r.Exam)
                .ThenInclude(e => e.TbQuestions)
                .Include(r => r.TbExamResultDetails)
                .ThenInclude(d => d.Question)
                .FirstOrDefaultAsync(r => r.Id == resultId);

            if (result == null)
                return NotFound();

            return View(result);
        }

        // GET: /quiz/create - Create new exam form
        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            ViewData["Subjects"] = await _context.TbSubjects.ToListAsync();
            return View();
        }

        // POST: /quiz/create - Submit new exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,SubjectId,Duration,Difficulty")] TbExam exam)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            try
            {
                exam.CreatedBy = userId.Value;
                exam.CreatedAt = DateTime.Now;
                exam.IsApproved = false;
                exam.IsPaid = false;

                _context.Add(exam);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đề thi đã được tạo thành công! Đang chờ duyệt từ admin.";
                return RedirectToAction("Index", "Profile", new { tab = "my-exams" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating exam: " + ex.Message);
            }

            ViewData["Subjects"] = await _context.TbSubjects.ToListAsync();
            return View(exam);
        }

        // GET: /quiz/myexams - View user's created exams
        public async Task<IActionResult> MyExams()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var userExams = await _context.TbExams
                .Where(e => e.CreatedBy == userId.Value)
                .Include(e => e.Subject)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(userExams);
        }

        // POST: /quiz/delete/{id} - Delete exam (only by creator)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var exam = await _context.TbExams
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
                return NotFound();

            // Only creator or admin can delete
            if (exam.CreatedBy != userId.Value)
                return Forbid();

            try
            {
                // Delete associated questions
                _context.TbQuestions.RemoveRange(exam.TbQuestions);
                
                // Delete exam
                _context.TbExams.Remove(exam);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đề thi đã được xóa thành công.";
                return RedirectToAction("Index", "Profile", new { tab = "my-exams" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting exam: " + ex.Message;
                return RedirectToAction("MyExams", "Quiz");
            }
        }

        // GET: /quiz/edit/{id} - Edit exam form
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var exam = await _context.TbExams.FirstOrDefaultAsync(e => e.Id == id);
            if (exam == null)
                return NotFound();

            // Only creator can edit
            if (exam.CreatedBy != userId.Value)
                return Forbid();

            ViewData["Subjects"] = await _context.TbSubjects.ToListAsync();
            return View(exam);
        }

        // POST: /quiz/edit/{id} - Save edited exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,SubjectId,Duration")] TbExam exam)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            if (id != exam.Id)
                return BadRequest();

            var existingExam = await _context.TbExams.FirstOrDefaultAsync(e => e.Id == id);
            if (existingExam == null)
                return NotFound();

            // Only creator can edit
            if (existingExam.CreatedBy != userId.Value)
                return Forbid();

            try
            {
                // Update only allowed fields
                existingExam.Title = exam.Title;
                existingExam.SubjectId = exam.SubjectId;
                existingExam.Duration = exam.Duration;

                _context.Update(existingExam);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đề thi đã được cập nhật thành công.";
                return RedirectToAction("Index", "Profile", new { tab = "my-exams" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TbExams.Any(e => e.Id == id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error updating exam: " + ex.Message);
            }

            ViewData["Subjects"] = await _context.TbSubjects.ToListAsync();
            return View(existingExam);
        }
    }
}
