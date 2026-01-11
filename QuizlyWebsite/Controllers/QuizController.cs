using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Services;

// Nam Khánh code đoạn này

namespace QuizlyWebsite.Controllers
{
    public class QuizController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IAccessControlService _accessControlService;
        private readonly IVNPayService _vnPayService;
        private readonly ILogger<QuizController> _logger;
        private readonly IXpService _xpService;
        private readonly IQuestionParserService _questionParserService;

        public QuizController(
            QuizlyDbContext context,
            ISubscriptionService subscriptionService,
            IAccessControlService accessControlService,
            IVNPayService vnPayService,
            ILogger<QuizController> logger,
            IXpService xpService,
            IQuestionParserService questionParserService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
            _accessControlService = accessControlService;
            _vnPayService = vnPayService;
            _logger = logger;
            _xpService = xpService;
            _questionParserService = questionParserService;
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

            if (exam.IsApproved != true)
            {
                if (!userId.HasValue || exam.CreatedBy != userId.Value)
                {
                    TempData["ErrorMessage"] = "Đề thi này đang chờ duyệt và chỉ có người tạo mới được xem.";
                    return RedirectToAction("List");
                }
            }

            if (exam.IsPaid == true)
            {
                if (userId.HasValue)
                {
                    var canAccess = await _accessControlService.CanAccessExamAsync(userId.Value, exam);
                    ViewBag.CanAccessPaidExam = canAccess;

                    var hasPurchase = await _context.TbUserPurchases
                        .AnyAsync(p => p.UserId == userId.Value &&
                                      p.ExamId == exam.Id &&
                                      (p.ExpiredAt == null || p.ExpiredAt > DateTime.UtcNow));
                    ViewBag.HasPurchase = hasPurchase;
                    ViewBag.ExamPrice = exam.Price ?? 0;
                }
                else
                {
                    ViewBag.CanAccessPaidExam = false;
                    ViewBag.ExamPrice = exam.Price ?? 0;
                }
            }
            else
            {
                ViewBag.CanAccessPaidExam = true;
            }

            return View(exam);
        }

        // POST: /quiz/purchase/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchaseExam(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var exam = await _context.TbExams.FindAsync(id);
            if (exam == null)
                return NotFound();

            if (exam.IsPaid != true)
            {
                TempData["ErrorMessage"] = "Đề thi này là miễn phí.";
                return RedirectToAction("Detail", new { id });
            }

            var hasSubscription = await _subscriptionService.CanAccessPremiumContentAsync(userId.Value);
            if (hasSubscription)
            {
                TempData["SuccessMessage"] = "Bạn đã có gói hội viên, không cần mua đề thi này.";
                return RedirectToAction("Detail", new { id });
            }

            var hasPurchase = await _context.TbUserPurchases
                .AnyAsync(p => p.UserId == userId.Value &&
                              p.ExamId == exam.Id &&
                              (p.ExpiredAt == null || p.ExpiredAt > DateTime.UtcNow));
            if (hasPurchase)
            {
                TempData["SuccessMessage"] = "Bạn đã mua đề thi này rồi.";
                return RedirectToAction("Detail", new { id });
            }

            var price = exam.Price ?? 0;
            if (price <= 0)
            {
                TempData["ErrorMessage"] = "Đề thi này không có giá.";
                return RedirectToAction("Detail", new { id });
            }

            var payment = new TbPayment
            {
                UserId = userId.Value,
                ExamId = id,
                Amount = price,
                Currency = "VND",
                Provider = "VNPay",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.TbPayments.Add(payment);
            await _context.SaveChangesAsync();

            // Get user info
            var user = await _context.TbUsers.FindAsync(userId.Value);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1" || string.IsNullOrEmpty(ipAddress))
                ipAddress = "127.0.0.1";
            var returnUrl = Url.Action("PurchaseCallback", "Quiz", new { id = exam.Id }, Request.Scheme) ?? "";

            var paymentUrl = _vnPayService.CreatePaymentUrl(
                orderId: payment.Id,
                orderCode: $"EXAM_{exam.Id}_{payment.Id}",
                amount: price,
                orderDescription: $"Mua de thi: {exam.Title}",
                customerName: user?.FullName,
                customerEmail: user?.Email,
                customerPhone: user?.PhoneNumber,
                ipAddress: ipAddress,
                returnUrl: returnUrl
            );

            return Redirect(paymentUrl);
        }

        // GET: /quiz/purchase-callback
        public async Task<IActionResult> PurchaseCallback(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var exam = await _context.TbExams.FindAsync(id);
            if (exam == null)
                return NotFound();

            var payment = await _context.TbPayments
                .Where(p => p.UserId == userId.Value && p.ExamId == id && p.Status == "Pending")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giao dịch thanh toán.";
                return RedirectToAction("Detail", new { id });
            }

            var vnpResponse = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var response = _vnPayService.ProcessPaymentResponse(vnpResponse);

            if (response.Success && response.ResponseCode == "00")
            {
                payment.Status = "Success";
                payment.ProviderTransId = response.TransactionId;

                var purchase = new TbUserPurchase
                {
                    UserId = userId.Value,
                    ExamId = id,
                    PurchasedAt = DateTime.UtcNow,
                    ExpiredAt = null // Mua một lần, không hết hạn
                };

                _context.TbUserPurchases.Add(purchase);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mua đề thi thành công! Bạn có thể bắt đầu làm bài ngay.";
                return RedirectToAction("Detail", new { id });
            }
            else
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();
                var errorMsg = response.ResponseCode == "00" ? "Lỗi xác thực chữ ký" : $"Mã lỗi: {response.ResponseCode}";
                TempData["ErrorMessage"] = $"Thanh toán thất bại: {errorMsg}";
                return RedirectToAction("Detail", new { id });
            }
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
                if (exam.IsApproved != true && exam.CreatedBy != userId)
                {
                    TempData["ErrorMessage"] = "Đề thi này đang chờ duyệt và chỉ có người tạo mới được thi.";
                    return RedirectToAction("List");
                }

                if (exam.IsPaid == true)
                {
                    var canAccess = await _accessControlService.CanAccessExamAsync(userId.Value, exam);
                    if (!canAccess)
                    {
                        TempData["ErrorMessage"] = "Bạn cần đăng ký gói hội viên để làm đề thi này.";
                        return RedirectToAction("Detail", new { id = id });
                    }
                }
            }

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
            decimal totalScore = 0;
            var resultDetails = new List<TbExamResultDetail>();

            // Calculate total possible points (sum of all question marks, or default to 10 if no marks set)
            decimal totalPossiblePoints = 0;
            foreach (var question in exam.TbQuestions)
            {
                if (question.Marks.HasValue && question.Marks.Value > 0)
                {
                    totalPossiblePoints += question.Marks.Value;
                }
                else
                {
                    // If no marks set, default to equal distribution: 10 points / total questions
                    totalPossiblePoints += 10m / exam.TbQuestions.Count;
                }
            }

            foreach (var question in exam.TbQuestions)
            {
                bool isCorrect = false;
                string? selectedOption = null;

                if (answers.TryGetValue(question.Id.ToString(), out var selected))
                {
                    selectedOption = selected;
                    // Compare with correct option (case-insensitive)
                    isCorrect = selectedOption?.Trim().ToUpperInvariant() == question.CorrectOption?.Trim().ToUpperInvariant();
                    if (isCorrect)
                    {
                        correctAnswers++;
                        // Add points for correct answer
                        if (question.Marks.HasValue && question.Marks.Value > 0)
                        {
                            totalScore += question.Marks.Value;
                        }
                        else
                        {
                            // If no marks set, default to equal distribution
                            totalScore += 10m / exam.TbQuestions.Count;
                        }
                    }
                }

                resultDetails.Add(new TbExamResultDetail
                {
                    QuestionId = question.Id,
                    SelectedOption = selectedOption,
                    IsCorrect = isCorrect
                });
            }

            // Apply penalty (subtract points from score)
            var score = Math.Max(0, totalScore - penaltyPoints);

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
                if (score >= 8m)
                {
                    var lesson = await _context.TbLessons.FindAsync(lessonId.Value);
                    if (lesson != null)
                    {
                        var progress = await _context.TbLessonProgresses
                            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.LessonId == lessonId.Value);

                        var isNewCompletion = false;
                        if (progress == null)
                        {
                            progress = new TbLessonProgress
                            {
                                UserId = userId.Value,
                                LessonId = lessonId.Value,
                                IsCompleted = true,
                                CompletedAt = DateTime.UtcNow
                            };
                            _context.TbLessonProgresses.Add(progress);
                            isNewCompletion = true;
                        }
                        else if (progress.IsCompleted != true)
                        {
                            progress.IsCompleted = true;
                            if (!progress.CompletedAt.HasValue)
                            {
                                progress.CompletedAt = DateTime.UtcNow;
                            }
                            _context.Update(progress);
                            isNewCompletion = true;
                        }

                        await _context.SaveChangesAsync();

                        // Award XP only if this is a new completion
                        if (isNewCompletion)
                        {
                            await _xpService.AddXpAsync(userId.Value, 10);
                        }
                    }

                    TempData["SuccessMessage"] = $"Chúc mừng! Bạn đã đạt {score:F1}/10 điểm. Bài học đã được đánh dấu hoàn thành! +10 XP";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Bạn đạt {score:F1}/10 điểm. Cần đạt ít nhất 8/10 điểm để hoàn thành bài học. Hãy làm lại!";
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

                return Json(new
                {
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

            return Json(new
            {
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
            var userId = HttpContext.Session.GetInt32("UserId");
            var result = await _context.TbExamResults
                .AsSplitQuery()
                .Include(r => r.Exam)
                .ThenInclude(e => e.TbQuestions)
                .Include(r => r.TbExamResultDetails)
                .ThenInclude(d => d.Question)
                .Include(r => r.Session)
                    .ThenInclude(s => s != null ? s.TbExamViolations : null!)
                .FirstOrDefaultAsync(r => r.Id == resultId);

            if (result == null)
                return NotFound();

            // Load user's review if exists
            TbExamReview? userReview = null;
            if (userId.HasValue)
            {
                userReview = await _context.TbExamReviews
                    .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ExamId == result.ExamId);
            }

            // Load other users' reviews (latest 10)
            var otherReviews = await _context.TbExamReviews
                .Where(r => r.ExamId == result.ExamId && (userId == null || r.UserId != userId.Value))
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.UserReview = userReview;
            ViewBag.OtherReviews = otherReviews;
            ViewBag.UserId = userId;

            return View(result);
        }

        // POST: /quiz/submit-review
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("quiz/submit-review")]
        public async Task<IActionResult> SubmitReview(int examId, int? rating, string? comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đánh giá đề thi.";
                return RedirectToAction("Detail", new { id = examId });
            }

            if (!rating.HasValue || rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn điểm đánh giá từ 1 đến 5 sao.";
                return RedirectToAction("List");
            }

            // Check if user already reviewed this exam
            var existingReview = await _context.TbExamReviews
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ExamId == examId);

            if (existingReview != null)
            {
                // Update existing review
                existingReview.Rating = rating.Value;
                existingReview.Comment = comment;
                existingReview.CreatedAt = DateTime.UtcNow;
                _context.TbExamReviews.Update(existingReview);
            }
            else
            {
                // Create new review
                var review = new TbExamReview
                {
                    UserId = userId.Value,
                    ExamId = examId,
                    Rating = rating.Value,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.TbExamReviews.AddAsync(review);
            }

            await _context.SaveChangesAsync();

            // Update exam rating
            var exam = await _context.TbExams.FindAsync(examId);
            var reviews = await _context.TbExamReviews
                .Where(r => r.ExamId == examId)
                .ToListAsync();

            if (reviews.Any() && exam != null)
            {
                exam.AvgRating = (double)(reviews.Average(r => r.Rating ?? 0));
                exam.TotalReviews = reviews.Count;
                _context.Update(exam);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá đề thi!";
            
            // Redirect back to result page if we have resultId
            var result = await _context.TbExamResults
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ExamId == examId && r.FinishedAt != null);
            
            if (result != null)
            {
                return RedirectToAction("Result", new { resultId = result.Id });
            }
            
            return RedirectToAction("Detail", new { id = examId });
        }

        // GET: /quiz/create - Redirect to profile page with my-exams tab
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Redirect to profile page with my-exams tab (form is integrated there)
            return RedirectToAction("Index", "Profile", new { tab = "my-exams" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("quiz/parse-questions-text")]
        public IActionResult ParseQuestionsText([FromBody] ParseTextRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            if (string.IsNullOrWhiteSpace(request.Text))
                return Json(new { success = false, message = "Vui lòng nhập nội dung đề thi" });

            try
            {
                var parseResult = _questionParserService.ParseFromText(request.Text);

                if (parseResult.ValidQuestions.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy câu hỏi hợp lệ trong văn bản",
                        errors = parseResult.Report.Errors.Select(e => $"Câu {e.QuestionNumber}: {e.ErrorReason}").ToList()
                    });
                }

                // Convert to simple format for frontend
                var questions = parseResult.ValidQuestions.Select(q => new
                {
                    question = q.Question,
                    optionA = q.OptionA,
                    optionB = q.OptionB,
                    optionC = q.OptionC,
                    optionD = q.OptionD,
                    correctOption = q.CorrectOption
                }).ToList();

                return Json(new
                {
                    success = true,
                    message = $"Đã parse thành công {questions.Count} câu hỏi",
                    questions = questions,
                    report = new
                    {
                        totalQuestions = parseResult.Report.TotalQuestions,
                        validCount = parseResult.Report.ValidCount,
                        errorCount = parseResult.Report.ErrorCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing questions from text");
                return Json(new { success = false, message = $"Lỗi khi parse: {ex.Message}" });
            }
        }

        // POST: /quiz/parse-questions-word - Parse questions from Word file (for user import)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10_000_000)] // 10MB limit
        [Route("quiz/parse-questions-word")]
        public async Task<IActionResult> ParseQuestionsWord(IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file Word" });

            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".docx")
                return Json(new { success = false, message = "Chỉ chấp nhận file Word (.docx)" });

            try
            {
                using var stream = file.OpenReadStream();
                var parseResult = await _questionParserService.ParseFromWordAsync(stream);

                if (parseResult.ValidQuestions.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy câu hỏi hợp lệ trong file Word",
                        errors = parseResult.Report.Errors.Select(e => $"Câu {e.QuestionNumber}: {e.ErrorReason}").ToList()
                    });
                }

                // Convert to simple format for frontend
                var questions = parseResult.ValidQuestions.Select(q => new
                {
                    question = q.Question,
                    optionA = q.OptionA,
                    optionB = q.OptionB,
                    optionC = q.OptionC,
                    optionD = q.OptionD,
                    correctOption = q.CorrectOption
                }).ToList();

                return Json(new
                {
                    success = true,
                    message = $"Đã parse thành công {questions.Count} câu hỏi",
                    questions = questions,
                    report = new
                    {
                        totalQuestions = parseResult.Report.TotalQuestions,
                        validCount = parseResult.Report.ValidCount,
                        errorCount = parseResult.Report.ErrorCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing questions from Word");
                return Json(new { success = false, message = $"Lỗi khi parse: {ex.Message}" });
            }
        }

        // Request model for parse text
        public class ParseTextRequest
        {
            public string Text { get; set; } = null!;
        }

        // POST: /quiz/create - Submit new exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string Title,
            int? CategoryId,
            string? NewCategoryTitle,
            int? SubjectId,
            string? NewSubjectTitle,
            string? NewSubjectDescription,
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
                bool useNewCategory = useNewCategoryValue == "true" || useNewCategoryValue == "on";

                if (useNewCategory)
                {
                    // Khi tick "Tạo mới": Chỉ cần kiểm tra NewCategoryTitle, HOÀN TOÀN BỎ QUA CategoryId
                    if (string.IsNullOrWhiteSpace(NewCategoryTitle))
                    {
                        ModelState.AddModelError("NewCategoryTitle", "Vui lòng nhập tên danh mục mới");
                    }
                    else
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
                }
                else
                {
                    // Khi KHÔNG tick "Tạo mới": Chỉ kiểm tra CategoryId từ dropdown
                    if (CategoryId.HasValue && CategoryId.Value > 0)
                    {
                        finalCategoryId = CategoryId.Value;
                    }
                    else
                    {
                        ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục");
                    }
                }

                // Handle Subject
                int finalSubjectId = 0;
                var useNewSubjectValue = form["UseNewSubject"].ToString();
                bool useNewSubject = useNewSubjectValue == "true" || useNewSubjectValue == "on";

                if (useNewSubject)
                {
                    // Khi tick "Tạo mới": Chỉ cần kiểm tra NewSubjectTitle, HOÀN TOÀN BỎ QUA SubjectId
                    if (string.IsNullOrWhiteSpace(NewSubjectTitle))
                    {
                        ModelState.AddModelError("NewSubjectTitle", "Vui lòng nhập tên môn học mới");
                    }
                    else if (finalCategoryId == 0)
                    {
                        ModelState.AddModelError("", "Cần có danh mục để tạo môn học mới. Vui lòng chọn hoặc tạo danh mục trước.");
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
                else
                {
                    // Khi KHÔNG tick "Tạo mới": Chỉ kiểm tra SubjectId từ dropdown
                    if (SubjectId.HasValue && SubjectId.Value > 0)
                    {
                        finalSubjectId = SubjectId.Value;
                    }
                    else
                    {
                        ModelState.AddModelError("SubjectId", "Vui lòng chọn môn học");
                    }
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

                // Normalize marks to ensure total = 10
                await NormalizeExamMarksAsync(exam.Id);

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

        // Helper method to normalize exam marks so total = 10
        // Distributes 10 points equally among all questions, rounded to 2 decimal places
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

        // GET: /quiz/myexams - Redirect to profile page (deprecated)
        [Route("quiz/myexams")]
        public IActionResult MyExams()
        {
            // Redirect to profile page with my-exams tab
            return RedirectToAction("Index", "Profile", new { tab = "my-exams" });
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
            string? NewCategoryTitle,
            int? SubjectId,
            string? NewSubjectTitle,
            string? NewSubjectDescription,
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
                bool useNewCategory = useNewCategoryValue == "true";

                if (!useNewCategory)
                {
                    ModelState.Remove("NewCategoryTitle");
                }

                if (useNewCategory)
                {
                    // Validate new category title
                    if (string.IsNullOrWhiteSpace(NewCategoryTitle))
                    {
                        ModelState.AddModelError("NewCategoryTitle", "Vui lòng nhập tên danh mục mới");
                    }
                    else
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
                        ModelState.AddModelError("CategoryId", "Vui lòng chọn hoặc tạo danh mục");
                    }
                }

                // Handle Subject
                int finalSubjectId = 0;
                var useNewSubjectValue = form["UseNewSubject"].ToString();
                bool useNewSubject = useNewSubjectValue == "true";

                // Clear validation errors for these fields if not using new subject
                if (!useNewSubject)
                {
                    ModelState.Remove("NewSubjectTitle");
                    ModelState.Remove("NewSubjectDescription");
                }

                if (useNewSubject)
                {
                    // Validate new subject title
                    if (string.IsNullOrWhiteSpace(NewSubjectTitle))
                    {
                        ModelState.AddModelError("NewSubjectTitle", "Vui lòng nhập tên môn học mới");
                    }
                    else if (finalCategoryId == 0)
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

                // Normalize marks to ensure total = 10
                await NormalizeExamMarksAsync(id);

                TempData["SuccessMessage"] = $"Đề thi đã được cập nhật thành công với {questions.Count} câu hỏi! Điểm số đã được tự động điều chỉnh để tổng = 10 điểm.";
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

        private async Task<int?> CreateCancelledExamResult(TbExamSession session, int userId)
        {
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

        [HttpPost]
        [Route("quiz/parse-text")]
        [Consumes("application/json")]
        public IActionResult ParseText([FromBody] ParseTextRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { error = "Vui lòng cung cấp nội dung văn bản" });
            }

            try
            {
                var result = _questionParserService.ParseFromText(request.Text);

                // Format kết quả theo yêu cầu
                var response = new
                {
                    validQuestions = result.ValidQuestions.Select(q => new
                    {
                        question = q.Question,
                        optionA = q.OptionA,
                        optionB = q.OptionB,
                        optionC = q.OptionC,
                        optionD = q.OptionD,
                        correctOption = q.CorrectOption
                    }).ToList(),
                    report = new
                    {
                        totalQuestions = result.Report.TotalQuestions,
                        validCount = result.Report.ValidCount,
                        errorCount = result.Report.ErrorCount,
                        errors = result.Report.Errors.Select(e => new
                        {
                            questionNumber = e.QuestionNumber,
                            questionContent = e.QuestionContent,
                            errorReason = e.ErrorReason
                        }).ToList()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi parse đề thi từ text");
                return StatusCode(500, new { error = $"Lỗi khi xử lý: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("quiz/parse-word")]
        [RequestSizeLimit(10_000_000)] // 10MB limit
        public async Task<IActionResult> ParseWord(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Vui lòng tải lên file Word (.docx)" });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".docx")
            {
                return BadRequest(new { error = "Chỉ chấp nhận file Word (.docx)" });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _questionParserService.ParseFromWordAsync(stream);

                var response = new
                {
                    validQuestions = result.ValidQuestions.Select(q => new
                    {
                        question = q.Question,
                        optionA = q.OptionA,
                        optionB = q.OptionB,
                        optionC = q.OptionC,
                        optionD = q.OptionD,
                        correctOption = q.CorrectOption
                    }).ToList(),
                    report = new
                    {
                        totalQuestions = result.Report.TotalQuestions,
                        validCount = result.Report.ValidCount,
                        errorCount = result.Report.ErrorCount,
                        errors = result.Report.Errors.Select(e => new
                        {
                            questionNumber = e.QuestionNumber,
                            questionContent = e.QuestionContent,
                            errorReason = e.ErrorReason
                        }).ToList()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi parse đề thi từ Word");
                return StatusCode(500, new { error = $"Lỗi khi xử lý file Word: {ex.Message}" });
            }
        }
    }

    public class ViolationRequest
    {
        public int SessionId { get; set; }
    }

    public class ParseTextRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}
