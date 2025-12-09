using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using QuizlyWebsite.Services;

namespace QuizlyWebsite.Controllers
{
    public class CourseController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<CourseController> _logger;
        private readonly IAccessControlService _accessControl;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILessonPreviewService _lessonPreviewService;

        public CourseController(
            QuizlyDbContext context, 
            ILogger<CourseController> logger,
            IAccessControlService accessControl,
            ISubscriptionService subscriptionService,
            ILessonPreviewService lessonPreviewService)
        {
            _context = context;
            _logger = logger;
            _accessControl = accessControl;
            _subscriptionService = subscriptionService;
            _lessonPreviewService = lessonPreviewService;
        }

        // GET: /courses
        [Route("courses")]
        public async Task<IActionResult> Index(string? search, string? priceFilter)
        {
            var query = _context.TbCourses
                .Where(c => c.IsApproved == true)
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.TbLessons)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title.Contains(search) || (c.Description != null && c.Description.Contains(search)));
            }

            if (priceFilter == "free")
            {
                query = query.Where(c => c.IsPaid == false || c.IsPaid == null);
            }
            else if (priceFilter == "paid")
            {
                query = query.Where(c => c.IsPaid == true);
            }

            var courses = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            ViewData["Search"] = search;
            ViewData["PriceFilter"] = priceFilter;

            return View(courses);
        }

        // GET: Course/Create
        public IActionResult Create()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description")] TbCourse course)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                course.CreatedBy = userId.Value;
                course.CreatedAt = DateTime.Now;
                course.IsApproved = false;

                _context.Add(course);
                await _context.SaveChangesAsync();

                // Auto-assign preview lessons if FreeLessonCount is set
                if (course.FreeLessonCount.HasValue && course.FreeLessonCount.Value > 0)
                {
                    await _lessonPreviewService.AssignPreviewLessonsAsync(course.Id);
                    _logger.LogInformation($"Auto-assigned {course.FreeLessonCount} preview lessons to course {course.Id}");
                }

                TempData["SuccessMessage"] = "Course created successfully! Waiting for admin approval.";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating course: {ex.Message}");
                ModelState.AddModelError("", "Error creating course");
            }

            return View(course);
        }

        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            if (id == null)
                return NotFound();

            var course = await _context.TbCourses.FindAsync(id);
            if (course == null)
                return NotFound();

            // Only allow creator to edit
            if (course.CreatedBy != userId.Value)
                return Forbid();

            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description")] TbCourse course)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            if (id != course.Id)
                return NotFound();

            try
            {
                var existingCourse = await _context.TbCourses.FindAsync(id);
                if (existingCourse == null)
                    return NotFound();

                // Only allow creator to edit
                if (existingCourse.CreatedBy != userId.Value)
                    return Forbid();

                existingCourse.Title = course.Title;
                existingCourse.Description = course.Description;

                _context.Update(existingCourse);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Course updated successfully!";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating course: {ex.Message}");
                ModelState.AddModelError("", "Error updating course");
            }

            return View(course);
        }

        // GET: Course/View/5
        public async Task<IActionResult> View(int? id)
        {
            if (id == null)
                return NotFound();

            var course = await _context.TbCourses
                .Include(c => c.TbLessons)
                    .ThenInclude(l => l.TbLessonProgresses)
                        .ThenInclude(p => p.User)
                .Include(c => c.CreatedByNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var userHasAccess = await _accessControl.CanAccessCourseAsync(userId, course);

            ViewBag.UserHasAccess = userHasAccess;
            ViewBag.UserId = userId;

            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var course = await _context.TbCourses.FindAsync(id);
            if (course == null)
                return NotFound();

            // Only allow creator to delete
            if (course.CreatedBy != userId.Value)
                return Forbid();

            try
            {
                _context.TbCourses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting course: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting course";
            }

            return RedirectToAction("Index", "Profile");
        }
    }
}
