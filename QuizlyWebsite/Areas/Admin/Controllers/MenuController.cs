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
    public class MenuController : Controller
    {
        private readonly QuizlyDbContext _context;

        public MenuController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: Admin/Menu
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var quizlyDbContext = _context.TbMenus.Include(t => t.Parent);
            return View(await quizlyDbContext.OrderBy(m => m.Order).ToListAsync());
        }

        // GET: Admin/Menu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbMenu = await _context.TbMenus
                .Include(t => t.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbMenu == null)
            {
                return NotFound();
            }

            return View(tbMenu);
        }

        // GET: Admin/Menu/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewData["ParentId"] = new SelectList(_context.TbMenus, "Id", "Title");
            return View();
        }

        // POST: Admin/Menu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Url,ParentId,Order,Location,CreatedAt")] TbMenu tbMenu)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Xử lý checkbox
            tbMenu.IsActive = Request.Form["IsActive"].ToString() == "true";

            if (ModelState.IsValid)
            {
                tbMenu.CreatedAt = DateTime.Now;
                _context.Add(tbMenu);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Menu đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentId"] = new SelectList(_context.TbMenus, "Id", "Title", tbMenu.ParentId);
            return View(tbMenu);
        }

        // GET: Admin/Menu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbMenu = await _context.TbMenus.FindAsync(id);
            if (tbMenu == null)
            {
                return NotFound();
            }
            ViewData["ParentId"] = new SelectList(_context.TbMenus.Where(m => m.Id != id), "Id", "Title", tbMenu.ParentId);
            return View(tbMenu);
        }

        // POST: Admin/Menu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Url,ParentId,Order,Location,CreatedAt")] TbMenu tbMenu)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbMenu.Id)
            {
                return NotFound();
            }

            // Xử lý checkbox
            tbMenu.IsActive = Request.Form["IsActive"].ToString() == "true";

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbMenu);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Menu đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbMenuExists(tbMenu.Id))
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
            ViewData["ParentId"] = new SelectList(_context.TbMenus.Where(m => m.Id != id), "Id", "Title", tbMenu.ParentId);
            return View(tbMenu);
        }

        // GET: Admin/Menu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbMenu = await _context.TbMenus
                .Include(t => t.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbMenu == null)
            {
                return NotFound();
            }

            return View(tbMenu);
        }

        // POST: Admin/Menu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbMenu = await _context.TbMenus.FindAsync(id);
            if (tbMenu != null)
            {
                _context.TbMenus.Remove(tbMenu);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Menu đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TbMenuExists(int id)
        {
            return _context.TbMenus.Any(e => e.Id == id);
        }
    }
}
