using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CoursesController : Controller
    {
        private readonly QuizlyDbContext _context;

        public CoursesController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/courses
        [Route("admin/courses")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var courses = await _context.TbCourses
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.TbLessons)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbCourses.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/Courses.cshtml", courses);
        }

        // GET: /admin/course-form or /admin/course-form/{id}
        [Route("admin/course-form")]
        [Route("admin/course-form/{id}")]
        public async Task<IActionResult> Form(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id.HasValue)
            {
                var course = await _context.TbCourses.FindAsync(id.Value);
                if (course == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/CourseForm.cshtml", course);
            }

            return View("~/Areas/Admin/Views/Home/CourseForm.cshtml", new TbCourse());
        }

        // POST: /admin/course-form or /admin/course-form/{id}
        [HttpPost]
        [Route("admin/course-form")]
        [Route("admin/course-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, string title, string? description, bool isPaid, int? freeLessonCount, IFormFile? imageFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError(nameof(title), "Tên khóa học không được để trống");

            if (!ModelState.IsValid)
            {
                var model = id.HasValue ? await _context.TbCourses.FindAsync(id) : new TbCourse();
                return View("~/Areas/Admin/Views/Home/CourseForm.cshtml", model);
            }

            try
            {
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
                        var model = id.HasValue ? await _context.TbCourses.FindAsync(id) : new TbCourse();
                        return View("~/Areas/Admin/Views/Home/CourseForm.cshtml", model);
                    }

                    // Validate file size (5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("imageFile", "File không được vượt quá 5MB");
                        var model = id.HasValue ? await _context.TbCourses.FindAsync(id) : new TbCourse();
                        return View("~/Areas/Admin/Views/Home/CourseForm.cshtml", model);
                    }

                    // Create images directory if it doesn't exist
                    var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "courses");
                    if (!Directory.Exists(imagesPath))
                    {
                        Directory.CreateDirectory(imagesPath);
                    }

                    // Generate unique filename
                    var fileName = $"course_{DateTime.Now.Ticks}{fileExtension}";
                    var filePath = Path.Combine(imagesPath, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    imageUrl = $"/images/courses/{fileName}";

                    // Delete old image if exists and updating
                    if (id.HasValue && id > 0)
                    {
                        var oldCourse = await _context.TbCourses.FindAsync(id.Value);
                        if (oldCourse != null && !string.IsNullOrEmpty(oldCourse.ImageUrl) && !oldCourse.ImageUrl.StartsWith("http"))
                        {
                            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldCourse.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                    }
                }

                if (id.HasValue && id > 0)
                {
                    var course = await _context.TbCourses.FindAsync(id.Value);
                    if (course == null) return NotFound();

                    course.Title = title;
                    course.Description = description;
                    course.IsPaid = isPaid;
                    course.FreeLessonCount = freeLessonCount;
                    if (imageUrl != null)
                    {
                        course.ImageUrl = imageUrl;
                    }

                    _context.TbCourses.Update(course);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Khóa học đã được cập nhật";
                }
                else
                {
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                    var course = new TbCourse
                    {
                        Title = title,
                        Description = description,
                        IsPaid = isPaid,
                        FreeLessonCount = freeLessonCount,
                        ImageUrl = imageUrl,
                        CreatedBy = userId,
                        IsApproved = true,
                        CreatedAt = DateTime.Now
                    };

                    await _context.TbCourses.AddAsync(course);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Khóa học mới đã được tạo";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                var model = id.HasValue ? await _context.TbCourses.FindAsync(id) : new TbCourse();
                return View("~/Areas/Admin/Views/Home/CourseForm.cshtml", model);
            }
        }

        [HttpPost]
        [Route("admin/delete-course")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var course = await _context.TbCourses.FindAsync(req.Id);
            if (course == null) return Json(new { success = false, message = "Khóa học không tồn tại" });

            try
            {
                _context.TbCourses.Remove(course);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Khóa học đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa khóa học: " + ex.Message });
            }
        }

        // GET: /admin/courses/{courseId}/lessons
        [Route("admin/courses/{courseId}/lessons")]
        public IActionResult Lessons(int courseId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            return RedirectToAction("Index", "Lessons", new { area = "Admin", courseId = courseId });
        }

        // GET: /admin/lesson-form or /admin/lesson-form/{id}
        [Route("admin/lesson-form")]
        [Route("admin/lesson-form/{id}")]
        public async Task<IActionResult> LessonForm(int? id, int? courseId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var courses = await _context.TbCourses.ToListAsync();
            ViewData["Courses"] = courses;

            if (id.HasValue)
            {
                var lesson = await _context.TbLessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == id.Value);
                if (lesson == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/LessonForm.cshtml", lesson);
            }

            var lessonModel = new TbLesson();
            if (courseId.HasValue)
            {
                lessonModel.CourseId = courseId.Value;
            }
            return View("~/Areas/Admin/Views/Home/LessonForm.cshtml", lessonModel);
        }

        // POST: /admin/lesson-form or /admin/lesson-form/{id}
        [HttpPost]
        [Route("admin/lesson-form")]
        [Route("admin/lesson-form/{id}")]
        public async Task<IActionResult> LessonFormPost(int? id, int courseId, string title, string content, bool isPreview)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError(nameof(title), "Tên bài học không được để trống");

            if (string.IsNullOrWhiteSpace(content))
                ModelState.AddModelError(nameof(content), "Nội dung bài học không được để trống");

            if (!ModelState.IsValid)
            {
                var courses = await _context.TbCourses.ToListAsync();
                ViewData["Courses"] = courses;
                var model = id.HasValue ? await _context.TbLessons.FindAsync(id) : new TbLesson { CourseId = courseId };
                return View("~/Areas/Admin/Views/Home/LessonForm.cshtml", model);
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var lesson = await _context.TbLessons.FindAsync(id.Value);
                    if (lesson == null) return NotFound();

                    lesson.Title = title;
                    lesson.Content = content;
                    lesson.IsPreview = isPreview;

                    _context.TbLessons.Update(lesson);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Bài học đã được cập nhật";
                    return RedirectToAction("Lessons", new { courseId = lesson.CourseId });
                }
                else
                {
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                    var lesson = new TbLesson
                    {
                        CourseId = courseId,
                        Title = title,
                        Content = content,
                        IsPreview = isPreview,
                        CreatedBy = userId,
                        IsApproved = true,
                        CreatedAt = DateTime.Now
                    };

                    await _context.TbLessons.AddAsync(lesson);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Bài học mới đã được tạo";
                    return RedirectToAction("Lessons", new { courseId = courseId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                var courses = await _context.TbCourses.ToListAsync();
                ViewData["Courses"] = courses;
                var model = id.HasValue ? await _context.TbLessons.FindAsync(id) : new TbLesson { CourseId = courseId };
                return View("~/Areas/Admin/Views/Home/LessonForm.cshtml", model);
            }
        }

        // POST: /admin/delete-lesson
        [HttpPost]
        [Route("admin/delete-lesson")]
        public async Task<IActionResult> DeleteLesson([FromBody] DeleteRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var lesson = await _context.TbLessons.FindAsync(req.Id);
            if (lesson == null) return Json(new { success = false, message = "Bài học không tồn tại" });

            try
            {
                var courseId = lesson.CourseId;
                _context.TbLessons.Remove(lesson);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Bài học đã được xóa thành công", courseId = courseId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa bài học: " + ex.Message });
            }
        }

        public class DeleteRequest { public int Id { get; set; } }
    }
}

