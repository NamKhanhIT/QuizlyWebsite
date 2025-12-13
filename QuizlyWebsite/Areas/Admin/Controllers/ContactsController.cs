using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public ContactsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/contacts
        [Route("admin/contacts")]
        public async Task<IActionResult> Index(int page = 1, string filter = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var query = _context.TbContacts.AsQueryable();

            // Filter by read status
            if (filter == "unread")
            {
                query = query.Where(c => c.IsRead != true);
            }
            else if (filter == "read")
            {
                query = query.Where(c => c.IsRead == true);
            }

            var total = await query.CountAsync();
            var contacts = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;
            ViewData["Filter"] = filter;
            ViewData["Total"] = total;
            ViewData["UnreadCount"] = await _context.TbContacts.CountAsync(c => c.IsRead != true);

            return View("~/Areas/Admin/Views/Home/Contacts.cshtml", contacts);
        }

        // GET: /admin/contact-detail/{id}
        [Route("admin/contact-detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var contact = await _context.TbContacts.FindAsync(id);
            if (contact == null)
                return NotFound();

            // Mark as read if not read yet
            if (contact.IsRead != true)
            {
                contact.IsRead = true;
                contact.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return View("~/Areas/Admin/Views/Home/ContactDetail.cshtml", contact);
        }

        // POST: /admin/contact-response/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/contact-response/{id}")]
        public async Task<IActionResult> SendResponse(int id, string response)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var contact = await _context.TbContacts.FindAsync(id);
            if (contact == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(response))
            {
                contact.Response = response.Trim();
                contact.RespondedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã lưu phản hồi thành công";
            }
            else
            {
                TempData["Error"] = "Vui lòng nhập nội dung phản hồi";
            }

            return RedirectToAction("Detail", new { id });
        }

        // POST: /admin/contact-mark-read/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/contact-mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Unauthorized" });

            var contact = await _context.TbContacts.FindAsync(id);
            if (contact == null)
                return Json(new { success = false, message = "Not found" });

            contact.IsRead = true;
            contact.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã đánh dấu đã đọc" });
        }

        // POST: /admin/contact-delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/contact-delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var contact = await _context.TbContacts.FindAsync(id);
            if (contact == null)
                return NotFound();

            try
            {
                _context.TbContacts.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa liên hệ thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}

