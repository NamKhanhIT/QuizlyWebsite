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
    public class LessonsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public LessonsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: Admin/Lessons
        public async Task<IActionResult> Index(int? courseId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var query = _context.TbLessons.Include(t => t.Course).Include(t => t.CreatedByNavigation).AsQueryable();

            if (courseId.HasValue)
            {
                query = query.Where(l => l.CourseId == courseId.Value);
                ViewData["CourseId"] = courseId.Value;
                var course = await _context.TbCourses.FindAsync(courseId.Value);
                ViewData["Course"] = course;
            }

            return View(await query.OrderBy(l => l.Id).ToListAsync());
        }

        // GET: Admin/Lessons/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbLesson = await _context.TbLessons
                .Include(t => t.Course)
                .Include(t => t.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbLesson == null)
            {
                return NotFound();
            }

            return View(tbLesson);
        }

        // GET: Admin/Lessons/Create
        public async Task<IActionResult> Create(int? courseId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewData["CourseId"] = new SelectList(_context.TbCourses, "Id", "Title", courseId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email");

            // If courseId is provided, load course info for preview logic
            if (courseId.HasValue)
            {
                var course = await _context.TbCourses.FindAsync(courseId.Value);
                if (course != null)
                {
                    ViewData["Course"] = course;
                    // Count existing lessons for preview logic
                    var existingLessonsCount = await _context.TbLessons
                        .Where(l => l.CourseId == courseId.Value)
                        .CountAsync();
                    ViewData["ExistingLessonsCount"] = existingLessonsCount;
                }
            }

            return View();
        }

        // POST: Admin/Lessons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbLesson = new TbLesson();

            // Bind basic fields from form
            tbLesson.Title = Request.Form["Title"];
            tbLesson.Content = Request.Form["Content"];
            if (int.TryParse(Request.Form["CourseId"], out int courseId))
                tbLesson.CourseId = courseId;

            // Get course information to determine preview logic
            var course = await _context.TbCourses.FindAsync(tbLesson.CourseId);
            if (course == null)
            {
                ModelState.AddModelError("", "Không tìm thấy khóa học.");
                ViewData["CourseId"] = new SelectList(_context.TbCourses, "Id", "Title", tbLesson.CourseId);
                ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email");
                return View(tbLesson);
            }

            // Determine IsPreview based on course type and current lessons
            if (!(course.IsPaid ?? false)) // Free course - all lessons are preview
            {
                tbLesson.IsPreview = true;
            }
            else // Paid course - check free lesson limit
            {
                var existingLessonsCount = await _context.TbLessons
                    .Where(l => l.CourseId == tbLesson.CourseId)
                    .CountAsync();

                // If within free lesson limit, allow preview
                tbLesson.IsPreview = existingLessonsCount < (course.FreeLessonCount ?? 0);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(tbLesson.Title))
                ModelState.AddModelError("Title", "Tiêu đề bài học là bắt buộc.");
            if (string.IsNullOrWhiteSpace(tbLesson.Content))
                ModelState.AddModelError("Content", "Nội dung bài học là bắt buộc.");
            if (tbLesson.CourseId == 0)
                ModelState.AddModelError("CourseId", "Khóa học là bắt buộc.");

            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                tbLesson.CreatedBy = userId;
                tbLesson.CreatedAt = DateTime.Now;
                // Admin created lessons are auto-approved, no approval process needed
                tbLesson.IsApproved = true;
                _context.Add(tbLesson);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bài học đã được tạo thành công";
                return RedirectToAction(nameof(Index), new { courseId = tbLesson.CourseId });
            }
            ViewData["CourseId"] = new SelectList(_context.TbCourses, "Id", "Title", tbLesson.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email");
            ViewData["Course"] = course; // Pass course info to view
            return View(tbLesson);
        }

        // GET: Admin/Lessons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbLesson = await _context.TbLessons.FindAsync(id);
            if (tbLesson == null)
            {
                return NotFound();
            }

            ViewData["CourseId"] = new SelectList(_context.TbCourses, "Id", "Title", tbLesson.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbLesson.CreatedBy);

            // Load course info for preview logic
            var course = await _context.TbCourses.FindAsync(tbLesson.CourseId);
            if (course != null)
            {
                ViewData["Course"] = course;
            }

            return View(tbLesson);
        }

        // POST: Admin/Lessons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbLesson = await _context.TbLessons.FindAsync(id);
            if (tbLesson == null)
            {
                return NotFound();
            }

            // Bind basic fields from form
            tbLesson.Title = Request.Form["Title"];
            tbLesson.Content = Request.Form["Content"];

            // Get course information to determine preview logic
            var course = await _context.TbCourses.FindAsync(tbLesson.CourseId);
            if (course == null)
            {
                ModelState.AddModelError("", "Không tìm thấy khóa học.");
                ViewData["CourseId"] = new SelectList(_context.TbCourses, "Id", "Title", tbLesson.CourseId);
                ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbLesson.CreatedBy);
                return View(tbLesson);
            }

            // Determine IsPreview based on course type and position in course
            if (!(course.IsPaid ?? false)) // Free course - all lessons are preview
            {
                tbLesson.IsPreview = true;
            }
            else // Paid course - check position against free lesson limit
            {
                // Count lessons before this one (by creation order or ID)
                var lessonsBeforeThis = await _context.TbLessons
                    .Where(l => l.CourseId == tbLesson.CourseId && l.Id < tbLesson.Id)
                    .CountAsync();

                // If within free lesson limit, allow preview
                tbLesson.IsPreview = lessonsBeforeThis < (course.FreeLessonCount ?? 0);
            }

            // Xử lý checkbox IsApproved (admin can override)
            tbLesson.IsApproved = Request.Form["IsApproved"].ToString() == "true";

            // Validate required fields
            if (string.IsNullOrWhiteSpace(tbLesson.Title))
                ModelState.AddModelError("Title", "Tiêu đề bài học là bắt buộc.");
            if (string.IsNullOrWhiteSpace(tbLesson.Content))
                ModelState.AddModelError("Content", "Nội dung bài học là bắt buộc.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbLesson);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Bài học đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbLessonExists(tbLesson.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { courseId = tbLesson.CourseId });
            }
            ViewData["CourseId"] = new SelectList(_context.TbCourses, "Id", "Title", tbLesson.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbLesson.CreatedBy);
            ViewData["Course"] = course; // Pass course info to view
            return View(tbLesson);
        }

        // GET: Admin/Lessons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbLesson = await _context.TbLessons
                .Include(t => t.Course)
                .Include(t => t.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbLesson == null)
            {
                return NotFound();
            }

            return View(tbLesson);
        }

        // POST: Admin/Lessons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbLesson = await _context.TbLessons.FindAsync(id);
            if (tbLesson != null)
            {
                _context.TbLessons.Remove(tbLesson);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bài học đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index), new { courseId = tbLesson?.CourseId });
        }

        private bool TbLessonExists(int id)
        {
            return _context.TbLessons.Any(e => e.Id == id);
        }
    }
}

