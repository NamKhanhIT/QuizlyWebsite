using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Services;

namespace QuizlyWebsite.Controllers
{
    public class QuizController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IAccessControlService _accessControlService;

        public QuizController(QuizlyDbContext context, ISubscriptionService subscriptionService, IAccessControlService accessControlService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
            _accessControlService = accessControlService;
        }

        // GET: /quiz/list
        public async Task<IActionResult> List(int? categoryId, string? search, int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var query = _context.TbExams.AsQueryable();

            // Chỉ hiển thị đề đã được duyệt, hoặc đề của chính người dùng (nếu chưa duyệt)
            if (userId.HasValue)
            {
                query = query.Where(e => e.IsApproved == true || e.CreatedBy == userId.Value);
            }
            else
            {
                // Người dùng chưa đăng nhập chỉ thấy đề đã duyệt
                query = query.Where(e => e.IsApproved == true);
            }

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
                .Include(e => e.CreatedByNavigation)
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
            var userId = HttpContext.Session.GetInt32("UserId");
            var exam = await _context.TbExams
                .Include(e => e.Subject)
                .ThenInclude(s => s.Category)
                .Include(e => e.TbExamReviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
                return NotFound();

            // Kiểm tra quyền xem: chỉ người tạo mới xem được đề chưa duyệt
            if (exam.IsApproved != true)
            {
                if (!userId.HasValue || exam.CreatedBy != userId.Value)
                {
                    TempData["ErrorMessage"] = "Đề thi này đang chờ duyệt và chỉ có người tạo mới được xem.";
                    return RedirectToAction("List");
                }
            }

            // Kiểm tra quyền truy cập đề trả phí
            if (exam.IsPaid == true && userId.HasValue)
            {
                var canAccess = await _accessControlService.CanAccessExamAsync(userId.Value, exam);
                ViewBag.CanAccessPaidExam = canAccess;
            }
            else
            {
                ViewBag.CanAccessPaidExam = true;
            }

            return View(exam);
        }

        // GET: /quiz/start/{id}
        public async Task<IActionResult> Start(int id, int? lessonId = null, int? courseId = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var exam = await _context.TbExams
                .Include(e => e.TbQuestions)
                .Include(e => e.TbPenaltyRules)
                .Include(e => e.Lesson)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
                return NotFound();

            // If this is a lesson quiz, skip some checks
            bool isLessonQuiz = exam.LessonId.HasValue || lessonId.HasValue;
            
            if (!isLessonQuiz)
            {
                // Kiểm tra quyền xem: chỉ người tạo mới xem được đề chưa duyệt
                if (exam.IsApproved != true && exam.CreatedBy != userId)
                {
                    TempData["ErrorMessage"] = "Đề thi này đang chờ duyệt và chỉ có người tạo mới được thi.";
                    return RedirectToAction("List");
                }

                // Kiểm tra quyền truy cập đề trả phí (chỉ cho exam thông thường, không phải lesson quiz)
                if (exam.IsPaid == true)
                {
                    var canAccess = await _accessControlService.CanAccessExamAsync(userId.Value, exam);
                    if (!canAccess)
                    {
                        TempData["ErrorMessage"] = "Bạn cần đăng ký gói trả phí để làm đề thi này.";
                        return RedirectToAction("Detail", new { id = id });
                    }
                }
            }

            // Kiểm tra đề có câu hỏi không
            if (exam.TbQuestions == null || !exam.TbQuestions.Any())
            {
                TempData["ErrorMessage"] = "Đề thi này chưa có câu hỏi. Vui lòng quay lại sau.";
                if (isLessonQuiz && lessonId.HasValue && courseId.HasValue)
                {
                    return RedirectToAction("Learn", "Lesson", new { courseId = courseId.Value, lessonId = lessonId.Value });
                }
                return RedirectToAction("Detail", new { id = id });
            }

            // Create exam session
            var session = new TbExamSession
            {
                UserId = userId.Value,
                ExamId = id,
                StartedAt = DateTime.Now,
                Status = "Ongoing"
            };

            _context.TbExamSessions.Add(session);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("SessionId", session.Id);

            // Pass penalty rules to view
            ViewBag.PenaltyRules = exam.TbPenaltyRules?.ToList() ?? new List<TbPenaltyRule>();
            ViewBag.SessionId = session.Id;
            ViewBag.LessonId = lessonId;
            ViewBag.CourseId = courseId;
            ViewBag.IsLessonQuiz = isLessonQuiz;

            return View(exam);
        }

        // POST: /quiz/submit
        [HttpPost]
        public async Task<IActionResult> Submit(int examId, int? lessonId = null, int? courseId = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var sessionId = HttpContext.Session.GetInt32("SessionId");

            var exam = await _context.TbExams
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
                return NotFound();

            // Parse form data - form uses "answers_questionId" format
            var formData = Request.Form;
            var answers = new Dictionary<string, string>();
            foreach (var key in formData.Keys)
            {
                if (key.StartsWith("answers_"))
                {
                    var questionId = key.Replace("answers_", "");
                    var value = formData[key].ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        answers[questionId] = value;
                    }
                }
            }

            // Get session and calculate penalty
            var session = sessionId.HasValue 
                ? await _context.TbExamSessions
                    .Include(s => s.TbExamViolations)
                    .FirstOrDefaultAsync(s => s.Id == sessionId.Value)
                : null;

            decimal penaltyPoints = 0;
            if (session != null && session.TbExamViolations != null)
            {
                var violationCount = session.TbExamViolations.Count;
                if (violationCount > 0)
                {
                    // Get penalty rules for this exam
                    var penaltyRule = await _context.TbPenaltyRules
                        .Where(p => p.ExamId == examId && p.Reason == "TabSwitch")
                        .FirstOrDefaultAsync();
                    
                    // Default to 0.5 points per violation if no rule found
                    decimal penaltyPerViolation = 0.5m;
                    if (penaltyRule != null && penaltyRule.PenaltyPercent.HasValue)
                    {
                        // PenaltyPercent is stored as percentage (0-100), convert to points
                        // If PenaltyPercent is 50, it means 0.5 points (50% of 1 point)
                        penaltyPerViolation = penaltyRule.PenaltyPercent.Value / 100m;
                    }
                    
                    // Apply penalty: 0.5 points per violation
                    penaltyPoints = penaltyPerViolation * violationCount;
                }
            }

            // Calculate score
            int correctAnswers = 0;
            var resultDetails = new List<TbExamResultDetail>();

            foreach (var question in exam.TbQuestions)
            {
                bool isCorrect = false;
                string? selectedOption = null;
                
                if (answers.TryGetValue(question.Id.ToString(), out var selected))
                {
                    selectedOption = selected;
                    // Compare with correct option (case-insensitive)
                    isCorrect = selectedOption?.Trim().ToUpperInvariant() == question.CorrectOption?.Trim().ToUpperInvariant();
                    if (isCorrect) correctAnswers++;
                }

                resultDetails.Add(new TbExamResultDetail
                {
                    QuestionId = question.Id,
                    SelectedOption = selectedOption,
                    IsCorrect = isCorrect
                });
            }

            // Calculate score on scale of 10 (thang điểm 10)
            var baseScore = exam.TbQuestions.Count > 0 
                ? (decimal)correctAnswers / exam.TbQuestions.Count * 10 
                : 0;
            
            // Apply penalty (subtract points from score)
            var score = Math.Max(0, baseScore - penaltyPoints);

            // Get actual start time from session
            var startedAt = session?.StartedAt ?? DateTime.Now.AddMinutes(-exam.Duration);

            // Calculate percentage score for lesson quiz check
            var percentageScore = exam.TbQuestions.Count > 0 
                ? (decimal)correctAnswers / exam.TbQuestions.Count * 100 
                : 0;

            var result = new TbExamResult
            {
                UserId = userId.Value,
                ExamId = examId,
                SessionId = sessionId,
                Score = score,
                CorrectAnswers = correctAnswers,
                TotalQuestions = exam.TbQuestions.Count,
                StartedAt = startedAt,
                FinishedAt = DateTime.Now,
                Status = score >= 5 ? "Passed" : "Failed"  // Thang điểm 10, đạt từ 5.0 trở lên
            };

            _context.TbExamResults.Add(result);
            await _context.SaveChangesAsync();

            foreach (var detail in resultDetails)
            {
                detail.ExamResultId = result.Id;
                _context.TbExamResultDetails.Add(detail);
            }

            await _context.SaveChangesAsync();
            
            // Update session status
            if (session != null)
            {
                session.Status = "Completed";
                _context.Update(session);
                await _context.SaveChangesAsync();
            }
            
            HttpContext.Session.Remove("SessionId");

            // If this is a lesson quiz, redirect back to lesson page
            if (lessonId.HasValue && courseId.HasValue)
            {
                if (percentageScore >= 70)
                {
                    TempData["SuccessMessage"] = $"Chúc mừng! Bạn đã đạt {percentageScore:F1}% và có thể hoàn thành bài học.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Bạn đạt {percentageScore:F1}%. Cần đạt ít nhất 70% để hoàn thành bài học. Hãy làm lại!";
                }
                return RedirectToAction("Learn", "Lesson", new { courseId = courseId.Value, lessonId = lessonId.Value });
            }

            return RedirectToAction("Result", new { resultId = result.Id });
        }

        // POST: /quiz/violation - Record tab switch violation
        [HttpPost]
        [Route("quiz/violation")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RecordViolation([FromBody] ViolationRequest request)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            if (request == null || request.SessionId == 0)
                return BadRequest();

            var session = await _context.TbExamSessions
                .Include(s => s.TbExamViolations)
                .Include(s => s.Exam)
                .ThenInclude(e => e.TbPenaltyRules)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId);

            if (session == null)
                return NotFound();

            // Check if user owns this session
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (session.UserId != userId)
                return Forbid();

            // Check if exam is already cancelled
            if (session.Status == "Cancelled")
            {
                return Json(new { success = true, cancelled = true, message = "Bài thi đã bị hủy." });
            }

            // Check violation count
            var violationCount = session.TbExamViolations?.Count ?? 0;
            
            // If already 3+ violations, cancel exam and save result
            if (violationCount >= 3)
            {
                session.Status = "Cancelled";
                _context.Update(session);
                await _context.SaveChangesAsync();
                
                // Create exam result to save history even when cancelled
                var resultId = await CreateCancelledExamResult(session, userId);
                int? nullableResultId = resultId;
                
                return Json(new { 
                    success = true, 
                    cancelled = true, 
                    message = "Bài thi đã bị hủy do vi phạm quá 3 lần.",
                    resultId = nullableResultId
                });
            }

            // Record violation
            var violation = new TbExamViolation
            {
                SessionId = request.SessionId,
                ViolationType = "TabSwitch",
                ViolationTime = DateTime.Now
            };

            _context.TbExamViolations.Add(violation);
            await _context.SaveChangesAsync();

            var newCount = violationCount + 1;
            var remaining = 3 - newCount;

            return Json(new { 
                success = true, 
                violationCount = newCount, 
                remaining = remaining,
                message = $"Cảnh báo! Bạn đã chuyển tab/thoát màn hình. Đã trừ 0.5 điểm. Còn lại {remaining} lần trước khi hủy bài thi." 
            });
        }

        // GET: /quiz/result/{resultId}
        [Route("quiz/result/{resultId}")]
        public async Task<IActionResult> Result(int resultId)
        {
            var result = await _context.TbExamResults
                .Include(r => r.Exam)
                .ThenInclude(e => e.TbQuestions)
                .Include(r => r.TbExamResultDetails)
                .ThenInclude(d => d.Question)
                .Include(r => r.Session)
                .ThenInclude(s => s.TbExamViolations)
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

        // Helper method to create exam result when exam is cancelled
        private async Task<int?> CreateCancelledExamResult(TbExamSession session, int userId)
        {
            // Check if result already exists for this session
            var existingResult = await _context.TbExamResults
                .FirstOrDefaultAsync(r => r.SessionId == session.Id);

            if (existingResult != null)
            {
                // Update existing result
                existingResult.Status = "Cancelled";
                existingResult.FinishedAt = DateTime.Now;
                _context.Update(existingResult);
                await _context.SaveChangesAsync();
                return existingResult.Id;
            }
            else
            {
                // Create new result for cancelled exam
                var exam = await _context.TbExams
                    .Include(e => e.TbQuestions)
                    .FirstOrDefaultAsync(e => e.Id == session.ExamId);

                if (exam != null)
                {
                    // Count answered questions (if any)
                    int correctAnswers = 0;
                    int totalQuestions = exam.TbQuestions?.Count ?? 0;

                    var result = new TbExamResult
                    {
                        UserId = userId,
                        ExamId = session.ExamId,
                        SessionId = session.Id,
                        Score = 0, // Cancelled exam gets 0 score
                        CorrectAnswers = correctAnswers,
                        TotalQuestions = totalQuestions,
                        StartedAt = session.StartedAt,
                        FinishedAt = DateTime.Now,
                        Status = "Cancelled" // Mark as cancelled
                    };

                    _context.TbExamResults.Add(result);
                    await _context.SaveChangesAsync();
                    return result.Id;
                }
            }

            return null;
        }
    }

    public class ViolationRequest
    {
        public int SessionId { get; set; }
    }
}
