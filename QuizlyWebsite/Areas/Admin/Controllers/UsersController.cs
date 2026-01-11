using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly QuizlyDbContext _context;

        public UsersController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            return View(await _context.TbUsers.OrderByDescending(u => u.CreatedAt ?? DateTime.MinValue).ToListAsync());
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbUser = await _context.TbUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbUser == null)
            {
                return NotFound();
            }

            return View(tbUser);
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            return View();
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Username,Email,PasswordHash,FullName,AvatarUrl,Role,CreatedAt,Address,PhoneNumber")] TbUser tbUser, string password)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(password))
                {
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    tbUser.PasswordHash = Convert.ToBase64String(hashedBytes);
                }
                tbUser.CreatedAt = DateTime.Now;
                _context.Add(tbUser);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Người dùng đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            return View(tbUser);
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbUser = await _context.TbUsers.FindAsync(id);
            if (tbUser == null)
            {
                return NotFound();
            }
            return View(tbUser);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,PasswordHash,FullName,AvatarUrl,Role,CreatedAt,Address,PhoneNumber")] TbUser tbUser, string password)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        using var sha256 = System.Security.Cryptography.SHA256.Create();
                        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                        tbUser.PasswordHash = Convert.ToBase64String(hashedBytes);
                    }
                    _context.Update(tbUser);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Người dùng đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbUserExists(tbUser.Id))
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
            return View(tbUser);
        }

        // GET: Admin/Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbUser = await _context.TbUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbUser == null)
            {
                return NotFound();
            }

            return View(tbUser);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbUser = await _context.TbUsers.FindAsync(id);
            if (tbUser != null)
            {
                _context.TbUsers.Remove(tbUser);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Người dùng đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TbUserExists(int id)
        {
            return _context.TbUsers.Any(e => e.Id == id);
        }
    }
}
