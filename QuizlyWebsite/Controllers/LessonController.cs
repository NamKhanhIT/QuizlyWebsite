using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using QuizlyWebsite.Services;

namespace QuizlyWebsite.Controllers
{
    public class LessonController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly IXpService _xpService;
        private readonly ILogger<LessonController> _logger;
        private readonly IAccessControlService _accessControl;
        private readonly ISubscriptionService _subscriptionService;

        public LessonController(
            QuizlyDbContext context, 
            IXpService xpService, 
            ILogger<LessonController> logger,
            IAccessControlService accessControl,
            ISubscriptionService subscriptionService)
        {
            _context = context;
            _xpService = xpService;
            _logger = logger;
            _accessControl = accessControl;
            _subscriptionService = subscriptionService;
        }

        // GET: Lesson/Create
        public async Task<IActionResult> Create(int? courseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            if (courseId == null)
                return NotFound();

            var course = await _context.TbCourses.FindAsync(courseId);
            if (course == null)
                return NotFound();

            // Only allow course creator to add lessons
            if (course.CreatedBy != userId.Value)
                return Forbid();

            ViewData["CourseId"] = courseId;
            ViewData["CourseName"] = course.Title;
            return View();
        }

        // POST: Lesson/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, [Bind("Title,Content")] TbLesson lesson)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var course = await _context.TbCourses.FindAsync(courseId);
            if (course == null)
                return NotFound();

            if (course.CreatedBy != userId.Value)
                return Forbid();

            try
            {
                lesson.CourseId = courseId;
                lesson.CreatedBy = userId.Value;
                lesson.CreatedAt = DateTime.Now;
                lesson.IsApproved = false;

                _context.Add(lesson);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Lesson created successfully! Waiting for admin approval.";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating lesson: {ex.Message}");
                ModelState.AddModelError("", "Error creating lesson");
            }

            ViewData["CourseId"] = courseId;
            return View(lesson);
        }

        // GET: /learn/{courseId}/{lessonId}
        [Route("learn/{courseId}/{lessonId}")]
        public async Task<IActionResult> Learn(int courseId, int lessonId)
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var course = await _context.TbCourses
                .Include(c => c.TbLessons)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound();

            var lesson = await _context.TbLessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId && l.CourseId == courseId);

            if (lesson == null)
                return NotFound();

            // Check access
            var canAccess = await _accessControl.CanAccessLessonAsync(userId, lesson);
            if (!canAccess && lesson.IsPreview != true)
            {
                TempData["ErrorMessage"] = "Bạn cần nâng cấp tài khoản để truy cập bài học này.";
                return RedirectToAction("View", "Course", new { id = courseId });
            }

            // Get all lessons for navigation
            var allLessons = course.TbLessons?.OrderBy(l => l.Id).ToList() ?? new List<TbLesson>();
            var currentIndex = allLessons.FindIndex(l => l.Id == lessonId);
            var previousLesson = currentIndex > 0 ? allLessons[currentIndex - 1] : null;
            var nextLesson = currentIndex < allLessons.Count - 1 ? allLessons[currentIndex + 1] : null;

            // Get user progress for all lessons
            var allProgresses = await _context.TbLessonProgresses
                .Where(p => p.UserId == userId && allLessons.Select(l => l.Id).Contains(p.LessonId))
                .ToListAsync();
            
            var lessonProgressDict = allProgresses.ToDictionary(p => p.LessonId, p => p);

            // Get current lesson progress
            var progress = lessonProgressDict.ContainsKey(lessonId) ? lessonProgressDict[lessonId] : null;

            // Calculate course progress
            var completedLessons = allProgresses.Count(p => p.IsCompleted == true);
            var totalLessons = allLessons.Count;
            var courseProgress = totalLessons > 0 ? (completedLessons * 100 / totalLessons) : 0;

            ViewBag.Course = course;
            ViewBag.AllLessons = allLessons;
            ViewBag.LessonProgressDict = lessonProgressDict;
            ViewBag.PreviousLesson = previousLesson;
            ViewBag.NextLesson = nextLesson;
            ViewBag.CurrentIndex = currentIndex;
            ViewBag.IsCompleted = progress?.IsCompleted ?? false;
            ViewBag.CourseProgress = courseProgress;
            ViewBag.CanAccess = canAccess;

            return View(lesson);
        }

        // GET: Lesson/View/5
        public async Task<IActionResult> View(int? id)
        {
            if (id == null)
                return NotFound();

            var lesson = await _context.TbLessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (lesson == null)
                return NotFound();

            // Only show approved lessons or to creator
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!lesson.IsApproved.GetValueOrDefault() && lesson.CreatedBy != userId)
                return Forbid();

            return View(lesson);
        }

        // GET: Lesson/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            if (id == null)
                return NotFound();

            var lesson = await _context.TbLessons.FindAsync(id);
            if (lesson == null)
                return NotFound();

            // Only allow creator to edit
            if (lesson.CreatedBy != userId.Value)
                return Forbid();

            return View(lesson);
        }

        // POST: Lesson/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content")] TbLesson lesson)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            if (id != lesson.Id)
                return NotFound();

            try
            {
                var existingLesson = await _context.TbLessons.FindAsync(id);
                if (existingLesson == null)
                    return NotFound();

                if (existingLesson.CreatedBy != userId.Value)
                    return Forbid();

                existingLesson.Title = lesson.Title;
                existingLesson.Content = lesson.Content;
                existingLesson.Title = lesson.Title;

                _context.Update(existingLesson);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Lesson updated successfully!";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating lesson: {ex.Message}");
                ModelState.AddModelError("", "Error updating lesson");
            }

            return View(lesson);
        }

        // POST: Lesson/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var lesson = await _context.TbLessons.FindAsync(id);
            if (lesson == null)
                return NotFound();

            if (lesson.CreatedBy != userId.Value)
                return Forbid();

            try
            {
                _context.TbLessons.Remove(lesson);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Lesson deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting lesson: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting lesson";
            }

            return RedirectToAction("Index", "Profile");
        }

        // POST: Lesson/Complete/5
        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            var lesson = await _context.TbLessons.FindAsync(id);
            if (lesson == null)
                return NotFound();

            try
            {
                var progress = await _context.TbLessonProgresses
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.LessonId == id);

                if (progress == null)
                {
                    progress = new TbLessonProgress
                    {
                        UserId = userId.Value,
                        LessonId = id,
                        IsCompleted = true,
                        CompletedAt = DateTime.Now
                    };
                    _context.Add(progress);

                    // Award 10 XP for completing lesson
                    await _xpService.AddXpAsync(userId.Value, 10);
                }
                else if (!progress.IsCompleted.GetValueOrDefault())
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.Now;
                    _context.Update(progress);

                    // Award 10 XP for completing lesson
                    await _xpService.AddXpAsync(userId.Value, 10);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Lesson completed! +10 XP awarded.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error completing lesson: {ex.Message}");
                TempData["ErrorMessage"] = "Error completing lesson";
            }

            return RedirectToAction("View", new { id });
        }
    }
}
