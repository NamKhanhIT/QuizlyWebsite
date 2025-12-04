using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly QuizlyDbContext _context;

        public CategoriesController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/categories
        [Route("admin/categories")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var categories = await _context.TbCategories
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbCategories.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/Categories.cshtml", categories);
        }

        // GET: /admin/category-form or /admin/category-form/{id}
        [Route("admin/category-form")]
        [Route("admin/category-form/{id}")]
        public async Task<IActionResult> Form(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id.HasValue)
            {
                var category = await _context.TbCategories.FindAsync(id.Value);
                if (category == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/CategoryForm.cshtml", category);
            }

            return View("~/Areas/Admin/Views/Home/CategoryForm.cshtml", new TbCategory());
        }

        // POST: /admin/category-form or /admin/category-form/{id}
        [HttpPost]
        [Route("admin/category-form")]
        [Route("admin/category-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, string title)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError(nameof(title), "Tên danh mục không được để trống");

            if (!ModelState.IsValid)
            {
                var model = id.HasValue ? await _context.TbCategories.FindAsync(id) : new TbCategory();
                return View("~/Areas/Admin/Views/Home/CategoryForm.cshtml", model);
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var category = await _context.TbCategories.FindAsync(id.Value);
                    if (category == null) return NotFound();
                    category.Title = title;

                    _context.TbCategories.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Danh mục đã được cập nhật";
                }
                else
                {
                    var category = new TbCategory
                    {
                        Title = title,
                        CreatedAt = DateTime.Now
                    };

                    await _context.TbCategories.AddAsync(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Danh mục mới đã được tạo";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                var model = id.HasValue ? await _context.TbCategories.FindAsync(id) : new TbCategory();
                return View("~/Areas/Admin/Views/Home/CategoryForm.cshtml", model);
            }
        }

        // POST: /admin/delete-category (used by AJAX)
        [HttpPost]
        [Route("admin/delete-category")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var category = await _context.TbCategories.FindAsync(req.Id);
            if (category == null) return Json(new { success = false, message = "Danh mục không tồn tại" });

            _context.TbCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Danh mục đã được xóa" });
        }

        public class DeleteRequest { public int Id { get; set; } }
    }
}
