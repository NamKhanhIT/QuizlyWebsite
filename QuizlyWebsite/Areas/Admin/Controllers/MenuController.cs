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
        public async Task<IActionResult> Create([Bind("Id,Title,Url,ParentId,Order,Location")] TbMenu tbMenu)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Xử lý IsActive từ form (checkbox + hidden input)
            var isActiveValue = Request.Form["IsActive"].ToString();
            tbMenu.IsActive = isActiveValue == "true";

            if (ModelState.IsValid)
            {
                // Tự động set CreatedAt
                tbMenu.CreatedAt = DateTime.Now;
                
                // Tự động set IsActive = true nếu chưa có giá trị (fallback)
                if (tbMenu.IsActive == null)
                {
                    tbMenu.IsActive = true;
                }

                // Tự động set Location = "HEADER" nếu chưa có giá trị
                if (string.IsNullOrWhiteSpace(tbMenu.Location))
                {
                    tbMenu.Location = "HEADER";
                }

                // Tự động set Order = 0 nếu chưa có giá trị
                if (tbMenu.Order == null)
                {
                    tbMenu.Order = 0;
                }

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Url,ParentId,Order,Location,IsActive,CreatedAt")] TbMenu tbMenu)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbMenu.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Load menu hiện tại từ database để giữ nguyên CreatedAt và các giá trị khác
                    var existingMenu = await _context.TbMenus.FindAsync(id);
                    if (existingMenu == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật các trường được phép sửa
                    existingMenu.Title = tbMenu.Title;
                    existingMenu.Url = tbMenu.Url;
                    existingMenu.ParentId = tbMenu.ParentId;
                    existingMenu.Order = tbMenu.Order;
                    existingMenu.Location = tbMenu.Location;
                    existingMenu.IsActive = tbMenu.IsActive;
                    // Giữ nguyên CreatedAt - không cập nhật

                    _context.Update(existingMenu);
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

            var tbMenu = await _context.TbMenus
                .Include(m => m.InverseParent)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (tbMenu != null)
            {
                // Xóa tất cả menu con trước (đệ quy)
                await DeleteMenuRecursive(tbMenu);
                
                await _context.SaveChangesAsync();
                TempData["Success"] = "Menu đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        // Hàm đệ quy để xóa menu con trước
        private async Task DeleteMenuRecursive(TbMenu menu)
        {
            // Load tất cả menu con
            var childMenus = await _context.TbMenus
                .Where(m => m.ParentId == menu.Id)
                .Include(m => m.InverseParent)
                .ToListAsync();

            // Xóa từng menu con (đệ quy)
            foreach (var child in childMenus)
            {
                await DeleteMenuRecursive(child);
            }

            // Sau khi xóa hết menu con, xóa menu cha
            _context.TbMenus.Remove(menu);
        }

        private bool TbMenuExists(int id)
        {
            return _context.TbMenus.Any(e => e.Id == id);
        }
    }
}
