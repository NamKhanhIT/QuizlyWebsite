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
                .AsSplitQuery()
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
                .AsSplitQuery()
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
                .AsSplitQuery()
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
                .AsSplitQuery()
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
                .AsSplitQuery()
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

            ViewData["Subjects"] = await _context.TbSubjects.Include(s => s.Category).ToListAsync();
            ViewData["Categories"] = await _context.TbCategories.ToListAsync();
            return View();
        }

        // POST: /quiz/create - Submit new exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string Title,
            int? CategoryId,
            string NewCategoryTitle,
            int? SubjectId,
            string NewSubjectTitle,
            string NewSubjectDescription,
            int Duration,
            string Difficulty,
            IFormCollection form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Title))
                {
                    ModelState.AddModelError("", "Tên đề thi không được để trống");
                }

                if (Duration < 5 || Duration > 300)
                {
                    ModelState.AddModelError("", "Thời gian phải từ 5 đến 300 phút");
                }

                // Handle Category
                int finalCategoryId = 0;
                var useNewCategoryValue = form["UseNewCategory"].ToString();
                bool useNewCategory = useNewCategoryValue == "true" && !string.IsNullOrWhiteSpace(NewCategoryTitle);
                
                if (useNewCategory)
                {
                    // Create new category
                    var newCategory = new TbCategory
                    {
                        Title = NewCategoryTitle.Trim(),
                        CreatedAt = DateTime.Now
                    };
                    _context.TbCategories.Add(newCategory);
                    await _context.SaveChangesAsync();
                    finalCategoryId = newCategory.Id;
                }
                else if (CategoryId.HasValue && CategoryId.Value > 0)
                {
                    finalCategoryId = CategoryId.Value;
                }
                else
                {
                    ModelState.AddModelError("", "Vui lòng chọn hoặc tạo danh mục");
                }

                // Handle Subject
                int finalSubjectId = 0;
                var useNewSubjectValue = form["UseNewSubject"].ToString();
                bool useNewSubject = useNewSubjectValue == "true" && !string.IsNullOrWhiteSpace(NewSubjectTitle);
                
                if (useNewSubject)
                {
                    if (finalCategoryId == 0)
                    {
                        ModelState.AddModelError("", "Cần có danh mục để tạo môn học mới");
                    }
                    else
                    {
                        // Create new subject
                        var newSubject = new TbSubject
                        {
                            Title = NewSubjectTitle.Trim(),
                            Description = NewSubjectDescription?.Trim(),
                            CategoryId = finalCategoryId
                        };
                        _context.TbSubjects.Add(newSubject);
                        await _context.SaveChangesAsync();
                        finalSubjectId = newSubject.Id;
                    }
                }
                else if (SubjectId.HasValue && SubjectId.Value > 0)
                {
                    finalSubjectId = SubjectId.Value;
                }
                else
                {
                    ModelState.AddModelError("", "Vui lòng chọn hoặc tạo môn học");
                }

                // Extract questions from form
                var questions = new List<QuestionViewModel>();
                var questionIndices = new List<int>();
                
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("Questions[") && key.Contains("].Content"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(key, @"Questions\[(\d+)\]");
                        if (match.Success)
                        {
                            int index = int.Parse(match.Groups[1].Value);
                            if (!questionIndices.Contains(index))
                            {
                                questionIndices.Add(index);
                            }
                        }
                    }
                }

                foreach (var index in questionIndices)
                {
                    var content = form[$"Questions[{index}].Content"].ToString();
                    var optionA = form[$"Questions[{index}].OptionA"].ToString();
                    var optionB = form[$"Questions[{index}].OptionB"].ToString();
                    var optionC = form[$"Questions[{index}].OptionC"].ToString();
                    var optionD = form[$"Questions[{index}].OptionD"].ToString();
                    var correctOption = form[$"Questions[{index}].CorrectOption"].ToString();
                    var marksStr = form[$"Questions[{index}].Marks"].ToString();

                    if (!string.IsNullOrWhiteSpace(content) &&
                        !string.IsNullOrWhiteSpace(optionA) &&
                        !string.IsNullOrWhiteSpace(optionB) &&
                        !string.IsNullOrWhiteSpace(optionC) &&
                        !string.IsNullOrWhiteSpace(optionD) &&
                        !string.IsNullOrWhiteSpace(correctOption))
                    {
                        decimal marks = 1m;
                        if (!string.IsNullOrWhiteSpace(marksStr) && decimal.TryParse(marksStr, out decimal parsedMarks))
                        {
                            marks = parsedMarks;
                        }

                        questions.Add(new QuestionViewModel
                        {
                            Content = content,
                            OptionA = optionA,
                            OptionB = optionB,
                            OptionC = optionC,
                            OptionD = optionD,
                            CorrectOption = correctOption,
                            Marks = marks
                        });
                    }
                }

                if (questions.Count == 0)
                {
                    ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 câu hỏi");
                }

                if (!ModelState.IsValid)
                {
                    ViewData["Subjects"] = await _context.TbSubjects.Include(s => s.Category).ToListAsync();
                    ViewData["Categories"] = await _context.TbCategories.ToListAsync();
                    return View();
                }

                // Create exam
                var exam = new TbExam
                {
                    Title = Title.Trim(),
                    SubjectId = finalSubjectId,
                    Duration = Duration,
                    Difficulty = Difficulty,
                    CreatedBy = userId.Value,
                    CreatedAt = DateTime.Now,
                    IsApproved = false,
                    IsPaid = false,
                    QuestionCount = questions.Count
                };

                _context.TbExams.Add(exam);
                await _context.SaveChangesAsync();

                // Create questions
                foreach (var q in questions)
                {
                    var question = new TbQuestion
                    {
                        ExamId = exam.Id,
                        Content = q.Content,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD,
                        CorrectOption = q.CorrectOption,
                        Marks = q.Marks
                    };
                    _context.TbQuestions.Add(question);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đề thi đã được tạo thành công với {questions.Count} câu hỏi! Đang chờ duyệt từ admin.";
                return RedirectToAction("Index", "Profile", new { tab = "my-exams" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi tạo đề thi: " + ex.Message);
            }

            ViewData["Subjects"] = await _context.TbSubjects.Include(s => s.Category).ToListAsync();
            ViewData["Categories"] = await _context.TbCategories.ToListAsync();
            return View();
        }

        // Helper class for question data
        private class QuestionViewModel
        {
            public int Id { get; set; }
            public string Content { get; set; } = null!;
            public string OptionA { get; set; } = null!;
            public string OptionB { get; set; } = null!;
            public string OptionC { get; set; } = null!;
            public string OptionD { get; set; } = null!;
            public string CorrectOption { get; set; } = null!;
            public decimal Marks { get; set; }
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

            var exam = await _context.TbExams
                .AsSplitQuery()
                .Include(e => e.Subject)
                .ThenInclude(s => s.Category)
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (exam == null)
                return NotFound();

            // Only creator can edit
            if (exam.CreatedBy != userId.Value)
                return Forbid();

            ViewData["Subjects"] = await _context.TbSubjects.Include(s => s.Category).ToListAsync();
            ViewData["Categories"] = await _context.TbCategories.ToListAsync();
            ViewData["Questions"] = exam.TbQuestions?.OrderBy(q => q.Id).ToList() ?? new List<TbQuestion>();
            
            return View(exam);
        }

        // POST: /quiz/edit/{id} - Save edited exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string Title,
            int? CategoryId,
            string NewCategoryTitle,
            int? SubjectId,
            string NewSubjectTitle,
            string NewSubjectDescription,
            int Duration,
            string Difficulty,
            IFormCollection form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var existingExam = await _context.TbExams
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (existingExam == null)
                return NotFound();

            // Only creator can edit
            if (existingExam.CreatedBy != userId.Value)
                return Forbid();

            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Title))
                {
                    ModelState.AddModelError("", "Tên đề thi không được để trống");
                }

                if (Duration < 5 || Duration > 300)
                {
                    ModelState.AddModelError("", "Thời gian phải từ 5 đến 300 phút");
                }

                // Handle Category
                int finalCategoryId = 0;
                var useNewCategoryValue = form["UseNewCategory"].ToString();
                bool useNewCategory = useNewCategoryValue == "true" && !string.IsNullOrWhiteSpace(NewCategoryTitle);
                
                if (useNewCategory)
                {
                    // Create new category
                    var newCategory = new TbCategory
                    {
                        Title = NewCategoryTitle.Trim(),
                        CreatedAt = DateTime.Now
                    };
                    _context.TbCategories.Add(newCategory);
                    await _context.SaveChangesAsync();
                    finalCategoryId = newCategory.Id;
                }
                else if (CategoryId.HasValue && CategoryId.Value > 0)
                {
                    finalCategoryId = CategoryId.Value;
                }
                else
                {
                    // Use existing category from current subject
                    var currentSubject = await _context.TbSubjects
                        .Include(s => s.Category)
                        .FirstOrDefaultAsync(s => s.Id == existingExam.SubjectId);
                    if (currentSubject != null)
                    {
                        finalCategoryId = currentSubject.CategoryId;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Vui lòng chọn hoặc tạo danh mục");
                    }
                }

                // Handle Subject
                int finalSubjectId = 0;
                var useNewSubjectValue = form["UseNewSubject"].ToString();
                bool useNewSubject = useNewSubjectValue == "true" && !string.IsNullOrWhiteSpace(NewSubjectTitle);
                
                if (useNewSubject)
                {
                    if (finalCategoryId == 0)
                    {
                        ModelState.AddModelError("", "Cần có danh mục để tạo môn học mới");
                    }
                    else
                    {
                        // Create new subject
                        var newSubject = new TbSubject
                        {
                            Title = NewSubjectTitle.Trim(),
                            Description = NewSubjectDescription?.Trim(),
                            CategoryId = finalCategoryId
                        };
                        _context.TbSubjects.Add(newSubject);
                        await _context.SaveChangesAsync();
                        finalSubjectId = newSubject.Id;
                    }
                }
                else if (SubjectId.HasValue && SubjectId.Value > 0)
                {
                    finalSubjectId = SubjectId.Value;
                }
                else
                {
                    finalSubjectId = existingExam.SubjectId;
                }

                // Extract questions from form
                var questions = new List<QuestionViewModel>();
                var questionIndices = new List<int>();
                
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("Questions[") && key.Contains("].Content"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(key, @"Questions\[(\d+)\]");
                        if (match.Success)
                        {
                            int index = int.Parse(match.Groups[1].Value);
                            if (!questionIndices.Contains(index))
                            {
                                questionIndices.Add(index);
                            }
                        }
                    }
                }

                foreach (var index in questionIndices)
                {
                    var questionIdStr = form[$"Questions[{index}].Id"].ToString();
                    int questionId = 0;
                    int.TryParse(questionIdStr, out questionId);
                    
                    var content = form[$"Questions[{index}].Content"].ToString();
                    var optionA = form[$"Questions[{index}].OptionA"].ToString();
                    var optionB = form[$"Questions[{index}].OptionB"].ToString();
                    var optionC = form[$"Questions[{index}].OptionC"].ToString();
                    var optionD = form[$"Questions[{index}].OptionD"].ToString();
                    var correctOption = form[$"Questions[{index}].CorrectOption"].ToString();
                    var marksStr = form[$"Questions[{index}].Marks"].ToString();

                    if (!string.IsNullOrWhiteSpace(content) &&
                        !string.IsNullOrWhiteSpace(optionA) &&
                        !string.IsNullOrWhiteSpace(optionB) &&
                        !string.IsNullOrWhiteSpace(optionC) &&
                        !string.IsNullOrWhiteSpace(optionD) &&
                        !string.IsNullOrWhiteSpace(correctOption))
                    {
                        decimal marks = 1m;
                        if (!string.IsNullOrWhiteSpace(marksStr) && decimal.TryParse(marksStr, out decimal parsedMarks))
                        {
                            marks = parsedMarks;
                        }

                        questions.Add(new QuestionViewModel
                        {
                            Id = questionId,
                            Content = content,
                            OptionA = optionA,
                            OptionB = optionB,
                            OptionC = optionC,
                            OptionD = optionD,
                            CorrectOption = correctOption,
                            Marks = marks
                        });
                    }
                }

                // Check for questions to delete
                var questionsToDelete = new List<int>();
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("QuestionsToDelete["))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(key, @"QuestionsToDelete\[(\d+)\]");
                        if (match.Success)
                        {
                            int questionId = int.Parse(match.Groups[1].Value);
                            questionsToDelete.Add(questionId);
                        }
                    }
                }

                if (questions.Count == 0)
                {
                    ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 câu hỏi");
                }

                if (!ModelState.IsValid)
                {
                    ViewData["Subjects"] = await _context.TbSubjects.Include(s => s.Category).ToListAsync();
                    ViewData["Categories"] = await _context.TbCategories.ToListAsync();
                    ViewData["Questions"] = existingExam.TbQuestions?.OrderBy(q => q.Id).ToList() ?? new List<TbQuestion>();
                    return View(existingExam);
                }

                // Update exam
                existingExam.Title = Title.Trim();
                existingExam.SubjectId = finalSubjectId;
                existingExam.Duration = Duration;
                existingExam.Difficulty = Difficulty;
                existingExam.QuestionCount = questions.Count;
                // Reset approval if exam was approved
                if (existingExam.IsApproved == true)
                {
                    existingExam.IsApproved = false;
                }

                _context.Update(existingExam);

                // Delete removed questions
                foreach (var questionId in questionsToDelete)
                {
                    var questionToDelete = await _context.TbQuestions.FindAsync(questionId);
                    if (questionToDelete != null && questionToDelete.ExamId == id)
                    {
                        _context.TbQuestions.Remove(questionToDelete);
                    }
                }

                // Update or create questions
                foreach (var q in questions)
                {
                    if (q.Id > 0)
                    {
                        // Update existing question
                        var existingQuestion = await _context.TbQuestions.FindAsync(q.Id);
                        if (existingQuestion != null && existingQuestion.ExamId == id)
                        {
                            existingQuestion.Content = q.Content;
                            existingQuestion.OptionA = q.OptionA;
                            existingQuestion.OptionB = q.OptionB;
                            existingQuestion.OptionC = q.OptionC;
                            existingQuestion.OptionD = q.OptionD;
                            existingQuestion.CorrectOption = q.CorrectOption;
                            existingQuestion.Marks = q.Marks;
                            _context.Update(existingQuestion);
                        }
                    }
                    else
                    {
                        // Create new question
                        var newQuestion = new TbQuestion
                        {
                            ExamId = id,
                            Content = q.Content,
                            OptionA = q.OptionA,
                            OptionB = q.OptionB,
                            OptionC = q.OptionC,
                            OptionD = q.OptionD,
                            CorrectOption = q.CorrectOption,
                            Marks = q.Marks
                        };
                        _context.TbQuestions.Add(newQuestion);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đề thi đã được cập nhật thành công với {questions.Count} câu hỏi!";
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
                ModelState.AddModelError("", "Lỗi khi cập nhật đề thi: " + ex.Message);
            }

            ViewData["Subjects"] = await _context.TbSubjects.Include(s => s.Category).ToListAsync();
            ViewData["Categories"] = await _context.TbCategories.ToListAsync();
            ViewData["Questions"] = existingExam.TbQuestions?.OrderBy(q => q.Id).ToList() ?? new List<TbQuestion>();
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
