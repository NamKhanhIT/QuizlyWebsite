using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
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

        // GET: /admin/subjects
        [Route("admin/subjects")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var subjects = await _context.TbSubjects
                .OrderBy(s => s.Id)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbSubjects.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/Subjects.cshtml", subjects);
        }

        // GET: /admin/subject-form or /admin/subject-form/{id}
        [Route("admin/subject-form")]
        [Route("admin/subject-form/{id}")]
        public async Task<IActionResult> Form(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Load categories for dropdown
            ViewData["Categories"] = await _context.TbCategories.OrderBy(c => c.Title).ToListAsync();

            if (id.HasValue)
            {
                var subject = await _context.TbSubjects
                    .Include(s => s.Category)
                    .FirstOrDefaultAsync(s => s.Id == id.Value);
                if (subject == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/SubjectForm.cshtml", subject);
            }

            return View("~/Areas/Admin/Views/Home/SubjectForm.cshtml", new TbSubject());
        }

        // POST: /admin/subject-form or /admin/subject-form/{id}
        [HttpPost]
        [Route("admin/subject-form")]
        [Route("admin/subject-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, string title, string description, int categoryId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Load categories for dropdown (in case of error)
            ViewData["Categories"] = await _context.TbCategories.OrderBy(c => c.Title).ToListAsync();

            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError(nameof(title), "Tên môn học không được để trống");

            if (categoryId <= 0)
                ModelState.AddModelError(nameof(categoryId), "Vui lòng chọn danh mục");

            // Verify category exists
            if (categoryId > 0)
            {
                var categoryExists = await _context.TbCategories.AnyAsync(c => c.Id == categoryId);
                if (!categoryExists)
                    ModelState.AddModelError(nameof(categoryId), "Danh mục không tồn tại");
            }

            if (!ModelState.IsValid)
            {
                var model = id.HasValue 
                    ? await _context.TbSubjects.Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == id) 
                    : new TbSubject();
                if (model == null) model = new TbSubject();
                return View("~/Areas/Admin/Views/Home/SubjectForm.cshtml", model);
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var subject = await _context.TbSubjects.FindAsync(id.Value);
                    if (subject == null) return NotFound();
                    subject.Title = title;
                    subject.Description = description;
                    subject.CategoryId = categoryId;

                    _context.TbSubjects.Update(subject);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Môn học đã được cập nhật thành công";
                }
                else
                {
                    var subject = new TbSubject
                    {
                        Title = title,
                        Description = description,
                        CategoryId = categoryId
                    };

                    await _context.TbSubjects.AddAsync(subject);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Môn học mới đã được tạo thành công";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                var model = id.HasValue 
                    ? await _context.TbSubjects.Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == id) 
                    : new TbSubject();
                if (model == null) model = new TbSubject();
                return View("~/Areas/Admin/Views/Home/SubjectForm.cshtml", model);
            }
        }

        // POST: /admin/delete-subject (used by AJAX)
        [HttpPost]
        [Route("admin/delete-subject")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var subject = await _context.TbSubjects.FindAsync(req.Id);
            if (subject == null) return Json(new { success = false, message = "Môn học không tồn tại" });

            _context.TbSubjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Môn học đã được xóa" });
        }

        public class DeleteRequest { public int Id { get; set; } }
    }
}
