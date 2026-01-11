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
    public class SubjectsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public SubjectsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: Admin/Subjects
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var quizlyDbContext = _context.TbSubjects.Include(t => t.Category);
            return View(await quizlyDbContext.OrderBy(s => s.Title).ToListAsync());
        }

        // GET: Admin/Subjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbSubject = await _context.TbSubjects
                .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbSubject == null)
            {
                return NotFound();
            }

            return View(tbSubject);
        }

        // GET: Admin/Subjects/Create
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewData["CategoryId"] = new SelectList(_context.TbCategories, "Id", "Title");
            return View();
        }

        // POST: Admin/Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CategoryId,Title,Description")] TbSubject tbSubject)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (ModelState.IsValid)
            {
                _context.Add(tbSubject);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Môn học đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.TbCategories, "Id", "Title", tbSubject.CategoryId);
            return View(tbSubject);
        }

        // GET: Admin/Subjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbSubject = await _context.TbSubjects.FindAsync(id);
            if (tbSubject == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.TbCategories, "Id", "Title", tbSubject.CategoryId);
            return View(tbSubject);
        }

        // POST: Admin/Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CategoryId,Title,Description")] TbSubject tbSubject)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbSubject.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbSubject);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Môn học đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbSubjectExists(tbSubject.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.TbCategories, "Id", "Title", tbSubject.CategoryId);
            return View(tbSubject);
        }

        // GET: Admin/Subjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbSubject = await _context.TbSubjects
                .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbSubject == null)
            {
                return NotFound();
            }

            return View(tbSubject);
        }

        // POST: Admin/Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbSubject = await _context.TbSubjects.FindAsync(id);
            if (tbSubject != null)
            {
                _context.TbSubjects.Remove(tbSubject);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Môn học đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TbSubjectExists(int id)
        {
            return _context.TbSubjects.Any(e => e.Id == id);
        }
    }
}
