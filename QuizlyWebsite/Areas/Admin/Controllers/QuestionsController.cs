using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using QuizlyWebsite.Services;
using QuizlyWebsite.Models.ViewModels;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuestionsController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly IQuestionParserService _questionParserService;

        public QuestionsController(QuizlyDbContext context, IQuestionParserService questionParserService)
        {
            _context = context;
            _questionParserService = questionParserService;
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
                // Normalize marks when loading the page to ensure total = 10
                await NormalizeExamMarksAsync(examId);

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
                ViewData["TotalQuestions"] = total;
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
        public async Task<IActionResult> Form(int? id, int examId = 0)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // If editing existing question, get examId from question
            if (id.HasValue && id > 0)
            {
                var question = await _context.TbQuestions.FindAsync(id.Value);
                if (question == null) return NotFound();

                // Use examId from query string if provided, otherwise use question's examId
                if (examId == 0)
                    examId = question.ExamId;
            }

            // Validate examId
            if (examId == 0)
            {
                TempData["Error"] = "Vui lòng chọn đề thi";
                return RedirectToAction("Index");
            }

            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null)
            {
                TempData["Error"] = "Đề thi không tồn tại";
                return RedirectToAction("Index");
            }

            ViewData["ExamId"] = examId;

            if (id.HasValue && id > 0)
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
        public async Task<IActionResult> FormPost(int? id, int examId, string content, string optionA, string optionB, string optionC, string optionD, string correctOption, decimal? marks)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Validate exam exists
            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null)
            {
                ModelState.AddModelError(string.Empty, "Đề thi không tồn tại");
                ViewData["ExamId"] = examId;
                var model = id.HasValue ? await _context.TbQuestions.FindAsync(id) : new TbQuestion { ExamId = examId };
                if (model != null)
                {
                    model.Content = content;
                    model.OptionA = optionA;
                    model.OptionB = optionB;
                    model.OptionC = optionC;
                    model.OptionD = optionD;
                    model.CorrectOption = correctOption;
                    model.Marks = marks;
                }
                return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", model ?? new TbQuestion { ExamId = examId });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(content))
                ModelState.AddModelError(nameof(content), "Nội dung câu hỏi không được để trống");

            if (string.IsNullOrWhiteSpace(optionA))
                ModelState.AddModelError(nameof(optionA), "Lựa chọn A không được để trống");

            if (string.IsNullOrWhiteSpace(optionB))
                ModelState.AddModelError(nameof(optionB), "Lựa chọn B không được để trống");

            if (string.IsNullOrWhiteSpace(optionC))
                ModelState.AddModelError(nameof(optionC), "Lựa chọn C không được để trống");

            if (string.IsNullOrWhiteSpace(optionD))
                ModelState.AddModelError(nameof(optionD), "Lựa chọn D không được để trống");

            if (string.IsNullOrWhiteSpace(correctOption))
                ModelState.AddModelError(nameof(correctOption), "Vui lòng chọn đáp án đúng");

            if (!string.IsNullOrWhiteSpace(correctOption) && !new[] { "A", "B", "C", "D" }.Contains(correctOption.ToUpper()))
                ModelState.AddModelError(nameof(correctOption), "Đáp án đúng phải là A, B, C hoặc D");

            if (marks.HasValue && marks < 0)
                ModelState.AddModelError(nameof(marks), "Điểm không được nhỏ hơn 0");

            if (!ModelState.IsValid)
            {
                ViewData["ExamId"] = examId;
                var model = id.HasValue ? await _context.TbQuestions.FindAsync(id) : new TbQuestion { ExamId = examId };
                if (model != null)
                {
                    model.Content = content;
                    model.OptionA = optionA;
                    model.OptionB = optionB;
                    model.OptionC = optionC;
                    model.OptionD = optionD;
                    model.CorrectOption = correctOption;
                    model.Marks = marks;
                }
                return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", model ?? new TbQuestion { ExamId = examId });
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var question = await _context.TbQuestions.FindAsync(id.Value);
                    if (question == null)
                    {
                        ModelState.AddModelError(string.Empty, "Câu hỏi không tồn tại");
                        ViewData["ExamId"] = examId;
                        return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", new TbQuestion { ExamId = examId });
                    }

                    question.Content = content?.Trim() ?? string.Empty;
                    question.OptionA = optionA?.Trim() ?? string.Empty;
                    question.OptionB = optionB?.Trim() ?? string.Empty;
                    question.OptionC = optionC?.Trim() ?? string.Empty;
                    question.OptionD = optionD?.Trim() ?? string.Empty;
                    question.CorrectOption = correctOption?.ToUpper();
                    question.Marks = marks ?? 1;

                    _context.TbQuestions.Update(question);
                    await _context.SaveChangesAsync();
                    
                    await NormalizeExamMarksAsync(examId);
                    
                    TempData["Success"] = "Câu hỏi đã được cập nhật thành công. Điểm số đã được tự động điều chỉnh để tổng = 10 điểm.";
                }
                else
                {
                    var question = new TbQuestion
                    {
                        ExamId = examId,
                        Content = content?.Trim() ?? string.Empty,
                        OptionA = optionA?.Trim() ?? string.Empty,
                        OptionB = optionB?.Trim() ?? string.Empty,
                        OptionC = optionC?.Trim() ?? string.Empty,
                        OptionD = optionD?.Trim() ?? string.Empty,
                        CorrectOption = correctOption?.ToUpper(),
                        Marks = marks ?? 1
                    };

                    await _context.TbQuestions.AddAsync(question);
                    await _context.SaveChangesAsync();
                    
                    // Normalize marks to ensure total = 10
                    await NormalizeExamMarksAsync(examId);
                    
                    TempData["Success"] = "Câu hỏi mới đã được tạo thành công. Điểm số đã được tự động điều chỉnh để tổng = 10 điểm.";
                }

                return RedirectToAction("Index", new { examId = examId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi: " + ex.Message);
                ViewData["ExamId"] = examId;
                var model = id.HasValue ? await _context.TbQuestions.FindAsync(id) : new TbQuestion { ExamId = examId };
                if (model != null)
                {
                    model.Content = content;
                    model.OptionA = optionA;
                    model.OptionB = optionB;
                    model.OptionC = optionC;
                    model.OptionD = optionD;
                    model.CorrectOption = correctOption;
                    model.Marks = marks;
                }
                return View("~/Areas/Admin/Views/Home/QuestionForm.cshtml", model ?? new TbQuestion { ExamId = examId });
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

            try
            {
                var examId = question.ExamId;

                // Delete related ExamResultDetails first (foreign key constraint)
                var resultDetails = await _context.TbExamResultDetails
                    .Where(erd => erd.QuestionId == req.Id)
                    .ToListAsync();

                if (resultDetails.Any())
                {
                    _context.TbExamResultDetails.RemoveRange(resultDetails);
                    await _context.SaveChangesAsync();
                }

                // Now delete the question
                _context.TbQuestions.Remove(question);
                await _context.SaveChangesAsync();

                // Normalize marks after deletion
                await NormalizeExamMarksAsync(examId);

                return Json(new { success = true, message = "Câu hỏi đã được xóa. Điểm số đã được tự động điều chỉnh để tổng = 10 điểm." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa câu hỏi: {ex.Message}" });
            }
        }

        // POST: /admin/import-questions
        [HttpPost]
        [Route("admin/import-questions")]
        public async Task<IActionResult> ImportQuestions([FromBody] ImportQuestionsRequest request)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (request == null || request.ExamId <= 0)
                return Json(new { success = false, message = "Invalid request" });

            var exam = await _context.TbExams.FindAsync(request.ExamId);
            if (exam == null)
                return Json(new { success = false, message = "Đề thi không tồn tại" });

            QuestionParseResult parseResult;

            try
            {
                if (!string.IsNullOrWhiteSpace(request.Text))
                {
                    // Parse from text
                    parseResult = _questionParserService.ParseFromText(request.Text);
                }
                else
                {
                    return Json(new { success = false, message = "Vui lòng cung cấp nội dung văn bản" });
                }

                // Save valid questions to database
                int savedCount = 0;
                foreach (var q in parseResult.ValidQuestions)
                {
                    var question = new TbQuestion
                    {
                        ExamId = request.ExamId,
                        Content = q.Question,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD,
                        CorrectOption = q.CorrectOption,
                        Marks = 1
                    };

                    await _context.TbQuestions.AddAsync(question);
                    savedCount++;
                }

                if (savedCount > 0)
                {
                    await _context.SaveChangesAsync();

                    // Normalize marks to ensure total = 10
                    await NormalizeExamMarksAsync(request.ExamId);

                    // Update exam question count
                    exam.QuestionCount = await _context.TbQuestions.CountAsync(q => q.ExamId == request.ExamId);
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = $"Đã import thành công {savedCount} câu hỏi",
                    report = new
                    {
                        totalQuestions = parseResult.Report.TotalQuestions,
                        validCount = parseResult.Report.ValidCount,
                        errorCount = parseResult.Report.ErrorCount,
                        savedCount = savedCount,
                        errors = parseResult.Report.Errors.Select(e => new
                        {
                            questionNumber = e.QuestionNumber,
                            questionContent = e.QuestionContent,
                            errorReason = e.ErrorReason
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi import: {ex.Message}" });
            }
        }

        // POST: /admin/import-questions-word
        [HttpPost]
        [Route("admin/import-questions-word")]
        [RequestSizeLimit(10_000_000)] // 10MB limit
        public async Task<IActionResult> ImportQuestionsWord(int examId, IFormFile file)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (examId <= 0)
                return Json(new { success = false, message = "Invalid exam ID" });

            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null)
                return Json(new { success = false, message = "Đề thi không tồn tại" });

            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file Word" });

            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".docx")
                return Json(new { success = false, message = "Chỉ chấp nhận file Word (.docx)" });

            try
            {
                using var stream = file.OpenReadStream();
                var parseResult = await _questionParserService.ParseFromWordAsync(stream);

                // Save valid questions to database
                int savedCount = 0;
                foreach (var q in parseResult.ValidQuestions)
                {
                    var question = new TbQuestion
                    {
                        ExamId = examId,
                        Content = q.Question,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD,
                        CorrectOption = q.CorrectOption,
                        Marks = 1
                    };

                    await _context.TbQuestions.AddAsync(question);
                    savedCount++;
                }

                if (savedCount > 0)
                {
                    await _context.SaveChangesAsync();

                    // Normalize marks to ensure total = 10
                    await NormalizeExamMarksAsync(examId);

                    // Update exam question count
                    exam.QuestionCount = await _context.TbQuestions.CountAsync(q => q.ExamId == examId);
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = $"Đã import thành công {savedCount} câu hỏi",
                    report = new
                    {
                        totalQuestions = parseResult.Report.TotalQuestions,
                        validCount = parseResult.Report.ValidCount,
                        errorCount = parseResult.Report.ErrorCount,
                        savedCount = savedCount,
                        errors = parseResult.Report.Errors.Select(e => new
                        {
                            questionNumber = e.QuestionNumber,
                            questionContent = e.QuestionContent,
                            errorReason = e.ErrorReason
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi import: {ex.Message}" });
            }
        }

        public class DeleteRequest { public int Id { get; set; } }

        public class ImportQuestionsRequest
        {
            public int ExamId { get; set; }
            public string Text { get; set; } = string.Empty;
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

        // POST: /admin/normalize-exam-marks/{examId}
        [HttpPost]
        [Route("admin/normalize-exam-marks/{examId}")]
        public async Task<IActionResult> NormalizeExamMarks(int examId)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            try
            {
                await NormalizeExamMarksAsync(examId);
                return Json(new { success = true, message = "Đã chuẩn hóa điểm số thành công. Tổng điểm = 10 điểm." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
