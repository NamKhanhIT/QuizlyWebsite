using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public async Task<IActionResult> Index(int page = 1, string filter = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var query = _context.TbExams
                .Include(e => e.Subject)
                .Include(e => e.CreatedByNavigation)
                .AsQueryable();

            // Filter by approval status
            if (filter == "pending")
                query = query.Where(e => e.IsApproved == false);
            else if (filter == "approved")
                query = query.Where(e => e.IsApproved == true);

            var exams = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await query.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;
            ViewData["Filter"] = filter;
            ViewData["PendingCount"] = await _context.TbExams.CountAsync(e => e.IsApproved == false);

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
                var exam = await _context.TbExams
                    .Include(e => e.Subject)
                    .FirstOrDefaultAsync(e => e.Id == id.Value);
                if (exam == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", exam);
            }

            return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", new TbExam());
        }

        // POST: /admin/exam-form or /admin/exam-form/{id}
        [HttpPost]
        [Route("admin/exam-form")]
        [Route("admin/exam-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, string title, int? subjectId, int questionCount, int duration, string difficulty, string? isPremium, decimal? price, IFormFile? imageFile, 
            string? useNewSubject = null, string? newSubjectTitle = null, int? newSubjectCategoryId = null,
            string? useNewCategory = null, string? newCategoryTitle = null)
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
                // Handle new category creation
                int? finalCategoryId = null;
                bool useNewCategoryValue = !string.IsNullOrEmpty(useNewCategory) && (useNewCategory == "true" || useNewCategory == "on");
                
                if (useNewCategoryValue)
                {
                    // Khi tick "Tạo mới": Chỉ cần kiểm tra newCategoryTitle, HOÀN TOÀN BỎ QUA categoryId
                    if (string.IsNullOrWhiteSpace(newCategoryTitle))
                    {
                        ModelState.AddModelError("newCategoryTitle", "Vui lòng nhập tên danh mục mới");
                        var subjects = await _context.TbSubjects.ToListAsync();
                        var categories = await _context.TbCategories.ToListAsync();
                        ViewData["Subjects"] = subjects;
                        ViewData["Categories"] = categories;
                        var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                        return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
                    }
                    
                    var newCategory = new TbCategory
                    {
                        Title = newCategoryTitle,
                        CreatedAt = DateTime.Now
                    };
                    await _context.TbCategories.AddAsync(newCategory);
                    await _context.SaveChangesAsync();
                    finalCategoryId = newCategory.Id;
                }
                // Note: Category không bắt buộc nếu không tick "Tạo mới" vì có thể lấy từ Subject

                // Handle new subject creation
                int? finalSubjectId = null;
                bool useNewSubjectValue = !string.IsNullOrEmpty(useNewSubject) && (useNewSubject == "true" || useNewSubject == "on");
                
                if (useNewSubjectValue)
                {
                    // Khi tick "Tạo mới": Chỉ cần kiểm tra newSubjectTitle, HOÀN TOÀN BỎ QUA subjectId
                    if (string.IsNullOrWhiteSpace(newSubjectTitle))
                    {
                        ModelState.AddModelError("newSubjectTitle", "Vui lòng nhập tên môn học mới");
                        var subjects = await _context.TbSubjects.ToListAsync();
                        var categories = await _context.TbCategories.ToListAsync();
                        ViewData["Subjects"] = subjects;
                        ViewData["Categories"] = categories;
                        var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                        return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
                    }
                    
                    // Tạo môn học mới hoàn toàn
                    if (finalCategoryId == null && newSubjectCategoryId.HasValue)
                    {
                        finalCategoryId = newSubjectCategoryId.Value;
                    }
                    
                    var newSubject = new TbSubject
                    {
                        Title = newSubjectTitle,
                        CategoryId = finalCategoryId ?? 1, // Default to 1 if no category
                        Description = null
                    };
                    await _context.TbSubjects.AddAsync(newSubject);
                    await _context.SaveChangesAsync();
                    finalSubjectId = newSubject.Id;
                }
                else
                {
                    // Khi KHÔNG tick "Tạo mới": Chỉ kiểm tra subjectId từ dropdown
                    if (!subjectId.HasValue || subjectId.Value == 0)
                    {
                        ModelState.AddModelError("subjectId", "Vui lòng chọn môn học");
                        var subjects = await _context.TbSubjects.ToListAsync();
                        var categories = await _context.TbCategories.ToListAsync();
                        ViewData["Subjects"] = subjects;
                        ViewData["Categories"] = categories;
                        var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                        return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
                    }
                    finalSubjectId = subjectId;
                }

                string? imageUrl = null;

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("imageFile", "Chỉ chấp nhận file JPG, PNG, GIF hoặc WEBP");
                        var subjects = await _context.TbSubjects.ToListAsync();
                        var categories = await _context.TbCategories.ToListAsync();
                        ViewData["Subjects"] = subjects;
                        ViewData["Categories"] = categories;
                        var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                        return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
                    }

                    // Validate file size (5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("imageFile", "File không được vượt quá 5MB");
                        var subjects = await _context.TbSubjects.ToListAsync();
                        var categories = await _context.TbCategories.ToListAsync();
                        ViewData["Subjects"] = subjects;
                        ViewData["Categories"] = categories;
                        var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                        return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
                    }

                    // Create images directory if it doesn't exist
                    var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "exams");
                    if (!Directory.Exists(imagesPath))
                    {
                        Directory.CreateDirectory(imagesPath);
                    }

                    // Generate unique filename
                    var fileName = $"exam_{DateTime.Now.Ticks}{fileExtension}";
                    var filePath = Path.Combine(imagesPath, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    imageUrl = $"/images/exams/{fileName}";

                    // Delete old image if exists and updating
                    if (id.HasValue && id > 0)
                    {
                        var oldExam = await _context.TbExams.FindAsync(id.Value);
                        if (oldExam != null && !string.IsNullOrEmpty(oldExam.ImageUrl) && !oldExam.ImageUrl.StartsWith("http"))
                        {
                            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldExam.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                    }
                }

                // Final validation: ensure we have a valid subjectId
                if (!finalSubjectId.HasValue || finalSubjectId.Value == 0)
                {
                    ModelState.AddModelError("subjectId", "Vui lòng chọn hoặc tạo môn học");
                    var subjects = await _context.TbSubjects.ToListAsync();
                    var categories = await _context.TbCategories.ToListAsync();
                    ViewData["Subjects"] = subjects;
                    ViewData["Categories"] = categories;
                    var model = id.HasValue ? await _context.TbExams.FindAsync(id) : new TbExam();
                    return View("~/Areas/Admin/Views/Home/ExamForm.cshtml", model);
                }

                if (id.HasValue && id > 0)
                {
                    var exam = await _context.TbExams.FindAsync(id.Value);
                    if (exam == null) return NotFound();

                    exam.Title = title;
                    exam.SubjectId = finalSubjectId.Value;
                    exam.QuestionCount = questionCount;
                    exam.Duration = duration;
                    exam.Difficulty = difficulty;
                    bool isPremiumValue = isPremium == "true" || isPremium == "True";
                    exam.IsPremium = isPremiumValue;
                    exam.Price = isPremiumValue ? (price ?? 0) : 0;
                    if (imageUrl != null)
                    {
                        exam.ImageUrl = imageUrl;
                    }

                    _context.TbExams.Update(exam);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đề thi đã được cập nhật";
                }
                else
                {
                    var exam = new TbExam
                    {
                        Title = title,
                        SubjectId = finalSubjectId.Value,
                        QuestionCount = questionCount,
                        Duration = duration,
                        Difficulty = difficulty,
                        IsPremium = isPremium == "true" || isPremium == "True",
                        Price = (isPremium == "true" || isPremium == "True") ? (price ?? 0) : 0,
                        ImageUrl = imageUrl,
                        CreatedAt = DateTime.Now,
                        CreatedBy = HttpContext.Session.GetInt32("UserId") ?? 1,
                        IsApproved = true // Admin tạo thì tự động approved
                    };

                    await _context.TbExams.AddAsync(exam);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đề thi mới đã được tạo";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
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

            var exam = await _context.TbExams
                .Include(e => e.TbExamSessions)
                .Include(e => e.TbExamResults)
                .Include(e => e.TbQuestions)
                .Include(e => e.TbUserPurchases)
                .Include(e => e.TbExamReviews)
                .FirstOrDefaultAsync(e => e.Id == req.Id);
            
            if (exam == null) return Json(new { success = false, message = "Đề thi không tồn tại" });

            try
            {
                // Xóa các bản ghi liên quan trước
                // Xóa Exam Sessions
                if (exam.TbExamSessions != null && exam.TbExamSessions.Any())
                {
                    _context.TbExamSessions.RemoveRange(exam.TbExamSessions);
                }

                // Xóa Exam Results
                if (exam.TbExamResults != null && exam.TbExamResults.Any())
                {
                    _context.TbExamResults.RemoveRange(exam.TbExamResults);
                }

                // Xóa Questions
                if (exam.TbQuestions != null && exam.TbQuestions.Any())
                {
                    _context.TbQuestions.RemoveRange(exam.TbQuestions);
                }

                // Xóa User Purchases
                if (exam.TbUserPurchases != null && exam.TbUserPurchases.Any())
                {
                    _context.TbUserPurchases.RemoveRange(exam.TbUserPurchases);
                }

                // Xóa Exam Reviews
                if (exam.TbExamReviews != null && exam.TbExamReviews.Any())
                {
                    _context.TbExamReviews.RemoveRange(exam.TbExamReviews);
                }

                // Xóa Payments liên quan (nếu có)
                var relatedPayments = await _context.TbPayments
                    .Where(p => p.ExamId == exam.Id)
                    .ToListAsync();
                if (relatedPayments.Any())
                {
                    _context.TbPayments.RemoveRange(relatedPayments);
                }

                // Cuối cùng mới xóa Exam
                _context.TbExams.Remove(exam);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đề thi đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa đề thi: " + ex.Message });
            }
        }

        // GET: /admin/exam-detail/{id}
        [Route("admin/exam-detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Normalize marks before displaying
            await NormalizeExamMarksAsync(id);

            var exam = await _context.TbExams
                .Include(e => e.Subject)
                .ThenInclude(s => s.Category)
                .Include(e => e.CreatedByNavigation)
                .Include(e => e.ApprovedByNavigation)
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
                return NotFound();

            return View("~/Areas/Admin/Views/Home/ExamDetail.cshtml", exam);
        }

        // POST: /admin/approve-exam
        [HttpPost]
        [Route("admin/approve-exam")]
        public async Task<IActionResult> ApproveExam([FromBody] DeleteRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var exam = await _context.TbExams.FindAsync(req.Id);
            if (exam == null) return Json(new { success = false, message = "Đề thi không tồn tại" });

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                exam.IsApproved = true;
                exam.ApprovedBy = userId;
                exam.ApprovedAt = DateTime.Now;

                _context.TbExams.Update(exam);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đề thi đã được duyệt thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi duyệt đề thi: " + ex.Message });
            }
        }

        public class DeleteRequest { public int Id { get; set; } }

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
    }
}
