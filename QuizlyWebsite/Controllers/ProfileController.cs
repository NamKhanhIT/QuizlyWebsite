using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using QuizlyWebsite.Services;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace QuizlyWebsite.Controllers
{
    public class ProfileController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly IXpService _xpService;
        private readonly ILogger<ProfileController> _logger;
        private readonly ISubscriptionService _subscriptionService;

        public ProfileController(
            QuizlyDbContext context, 
            IXpService xpService, 
            ILogger<ProfileController> logger,
            ISubscriptionService subscriptionService)
        {
            _context = context;
            _xpService = xpService;
            _logger = logger;
            _subscriptionService = subscriptionService;
        }

        // GET: Profile - Dashboard with tabs
        [Route("profile")]
        [Route("user/profile")]
        public async Task<IActionResult> Index(string tab = "overview")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var user = await _context.TbUsers.FindAsync(userId);
            if (user == null)
                return NotFound();

            ViewData["CurrentTab"] = tab;
            ViewBag.User = user;

            // Load subscription info
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
            ViewBag.ActiveSubscription = subscription;
            ViewBag.IsPremium = await _subscriptionService.IsPremiumPlanAsync(userId.Value);

            // Always load all base data that all tabs need
            var userXp = await _xpService.GetUserXpAsync(userId.Value);
            var courses = await _context.TbCourses
                .Where(c => c.CreatedBy == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            var lessons = await _context.TbLessons
                .Where(l => l.CreatedBy == userId)
                .Include(l => l.Course)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            var exams = await _context.TbExams
                .Where(e => e.CreatedBy == userId)
                .Include(e => e.Subject)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewBag.UserCourses = courses;
            ViewBag.UserLessons = lessons;
            ViewBag.UserExams = exams;

            // Load learning progress - only count non-preview lessons
            var progress = await _context.TbLessonProgresses
                .Where(p => p.UserId == userId && p.IsCompleted == true)
                .Include(p => p.Lesson)
                .ThenInclude(l => l.Course)
                .OrderByDescending(p => p.CompletedAt)
                .ToListAsync();

            // Only count non-preview lessons
            var totalNonPreviewLessonsAvailable = await _context.TbLessons
                .Where(l => l.IsPreview != true)
                .CountAsync();
            var nonPreviewLessonsCompleted = progress
                .Where(p => p.Lesson != null && p.Lesson.IsPreview != true)
                .Select(p => p.LessonId)
                .Distinct()
                .Count();
            var xpPercentage = userXp != null ? (userXp.Xp % 1000) / 10 : 0;

            ViewBag.LearningProgress = new
            {
                TotalXP = userXp?.Xp ?? 0,
                Level = userXp?.Level ?? 1,
                LessonsCompleted = nonPreviewLessonsCompleted,
                TotalLessons = totalNonPreviewLessonsAvailable,
                XPPercentage = xpPercentage,
                CompletionPercentage = totalNonPreviewLessonsAvailable > 0 ? (nonPreviewLessonsCompleted * 100 / totalNonPreviewLessonsAvailable) : 0
            };

            // Load statistics
            var stats = new
            {
                CoursesCreated = courses.Count,
                CoursesApproved = courses.Count(c => c.IsApproved == true),
                CoursesPending = courses.Count(c => c.IsApproved == false),
                
                LessonsCreated = lessons.Count,
                LessonsApproved = lessons.Count(l => l.IsApproved == true),
                LessonsPending = lessons.Count(l => l.IsApproved == false),
                
                ExamsCreated = exams.Count,
                ExamsApproved = exams.Count(e => e.IsApproved == true),
                ExamsPending = exams.Count(e => e.IsApproved == false),
                
                LessonsCompleted = nonPreviewLessonsCompleted,
            };

            var avgRating = exams
                .Where(e => e.AvgRating.HasValue)
                .Average(e => (double?)e.AvgRating) ?? 0;

            ViewBag.Statistics = new
            {
                stats = stats,
                AverageRating = avgRating,
                UserXP = userXp?.Xp ?? 0
            };

            // Load exam results
            var examResults = await _context.TbExamResults
                .Where(r => r.UserId == userId)
                .Include(r => r.Exam)
                .OrderByDescending(r => r.FinishedAt)
                .ToListAsync();

            ViewBag.ExamResults = examResults;

            // Load payment history
            var payments = await _context.TbPayments
                .Where(p => p.UserId == userId)
                .Include(p => p.Exam)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Payments = payments;

            // Load courses with progress for learning history
            var enrolledCourses = await _context.TbCourses
                .Where(c => c.TbLessons.Any(l => l.TbLessonProgresses.Any(p => p.UserId == userId)))
                .Include(c => c.TbLessons)
                    .ThenInclude(l => l.TbLessonProgresses)
                .ToListAsync();

            var coursesWithProgress = new List<object>();
            foreach (var course in enrolledCourses)
            {
                var courseLessons = course.TbLessons?.ToList() ?? new List<TbLesson>();
                // Only count non-preview lessons
                var nonPreviewLessons = courseLessons.Where(l => l.IsPreview != true).ToList();
                var completedNonPreviewLessons = nonPreviewLessons
                    .Count(l => l.TbLessonProgresses?.Any(p => p.UserId == userId && p.IsCompleted == true) == true);
                var totalNonPreviewLessons = nonPreviewLessons.Count;
                var progressPercent = totalNonPreviewLessons > 0 ? (completedNonPreviewLessons * 100 / totalNonPreviewLessons) : 0;
                var currentLesson = nonPreviewLessons.FirstOrDefault(l => l.TbLessonProgresses?.Any(p => p.UserId == userId && p.IsCompleted != true) == true);

                coursesWithProgress.Add(new
                {
                    Course = (TbCourse)course,
                    ProgressPercent = progressPercent,
                    CompletedLessons = completedNonPreviewLessons,
                    TotalLessons = totalNonPreviewLessons,
                    CurrentLesson = (TbLesson?)currentLesson
                });
            }

            ViewBag.CoursesWithProgress = coursesWithProgress;

            // Load subjects for create exam form
            var subjects = await _context.TbSubjects
                .Include(s => s.Category)
                .OrderBy(s => s.Title)
                .ToListAsync();

            ViewBag.Subjects = subjects;

            return View("~/Views/User/Profile.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, string phone, string bio)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var user = await _context.TbUsers.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Tên hiển thị và Email là bắt buộc.";
                return RedirectToAction("Index", new { tab = "profile" });
            }

            if (await _context.TbUsers.AnyAsync(u => u.Email == email && u.Id != userId))
            {
                TempData["Error"] = "Email đã được sử dụng.";
                return RedirectToAction("Index", new { tab = "profile" });
            }

            user.FullName = fullName.Trim();
            user.Email = email.Trim();
            // Note: Phone and Bio fields would need to be added to TbUser model if they don't exist

            _context.TbUsers.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction("Index", new { tab = "profile" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var user = await _context.TbUsers.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng" });

            if (avatarFile == null || avatarFile.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file ảnh" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, message = "Chỉ chấp nhận file JPG, PNG hoặc GIF" });
            }

            // Validate file size (2MB)
            if (avatarFile.Length > 2 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File không được vượt quá 2MB" });
            }

            try
            {
                // Create images directory if it doesn't exist
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // Generate unique filename
                var fileName = $"avatar_{userId}_{DateTime.Now.Ticks}{fileExtension}";
                var filePath = Path.Combine(imagesPath, fileName);

                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new avatar
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                // Update user avatar URL
                user.AvatarUrl = $"/images/avatars/{fileName}";
                _context.TbUsers.Update(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật avatar thành công", avatarUrl = user.AvatarUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return Json(new { success = false, message = "Lỗi khi tải lên avatar: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var user = await _context.TbUsers.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "Tất cả các trường mật khẩu là bắt buộc.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            // Verify current password
            string currentPasswordHash = HashPassword(currentPassword);
            if (user.PasswordHash != currentPasswordHash)
            {
                TempData["Error"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và xác nhận không khớp.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            user.PasswordHash = HashPassword(newPassword);

            _context.TbUsers.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Index", new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfile(string confirmText)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var user = await _context.TbUsers.FindAsync(userId);
            if (user == null)
                return NotFound();

            try
            {
                // Delete user's avatar file if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(avatarPath))
                    {
                        System.IO.File.Delete(avatarPath);
                    }
                }

                // Note: In a real application, you might want to soft delete or handle cascading deletes
                // For now, we'll just remove the user (assuming FK constraints allow it)
                // You may need to delete related records first depending on your DB constraints

                _context.TbUsers.Remove(user);
                await _context.SaveChangesAsync();

                // Clear session
                HttpContext.Session.Clear();

                TempData["Success"] = "Tài khoản đã được xóa thành công.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user profile");
                TempData["Error"] = "Không thể xóa tài khoản. Vui lòng liên hệ admin.";
                return RedirectToAction("Index", new { tab = "profile" });
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
