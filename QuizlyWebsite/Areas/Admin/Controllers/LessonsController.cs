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
        public IActionResult Create(int? courseId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewData["CourseId"] = new SelectList(_context.TbCourses, "Id", "Title", courseId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email");
            return View();
        }

        // POST: Admin/Lessons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CourseId,Title,Content,CreatedBy,CreatedAt")] TbLesson tbLesson)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Xử lý checkbox
            tbLesson.IsPreview = Request.Form["IsPreview"].ToString() == "true";

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
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbLesson.CreatedBy);
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
            return View(tbLesson);
        }

        // POST: Admin/Lessons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,Title,Content,CreatedBy,CreatedAt")] TbLesson tbLesson)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbLesson.Id)
            {
                return NotFound();
            }

            // Xử lý checkbox
            tbLesson.IsApproved = Request.Form["IsApproved"].ToString() == "true";
            tbLesson.IsPreview = Request.Form["IsPreview"].ToString() == "true";

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

