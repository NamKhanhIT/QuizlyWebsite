using System;
using System.Linq;
using System.Threading.Tasks;
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

        // GET: Admin/Contacts
        public async Task<IActionResult> Index(string filter = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var query = _context.TbContacts.AsQueryable();

            if (filter == "unread")
                query = query.Where(c => c.IsRead != true);
            else if (filter == "read")
                query = query.Where(c => c.IsRead == true);

            ViewData["Filter"] = filter;
            return View(await query.OrderByDescending(c => c.CreatedAt).ToListAsync());
        }

        // GET: Admin/Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbContact = await _context.TbContacts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbContact == null)
            {
                return NotFound();
            }

            if (tbContact.IsRead != true)
            {
                tbContact.IsRead = true;
                tbContact.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return View(tbContact);
        }

        // GET: Admin/Contacts/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            return View();
        }

        // POST: Admin/Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Phone,Subject,Message,CreatedAt,IsRead,ReadAt,Response,RespondedAt")] TbContact tbContact)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (ModelState.IsValid)
            {
                tbContact.CreatedAt = DateTime.Now;
                _context.Add(tbContact);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Liên hệ đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            return View(tbContact);
        }

        // GET: Admin/Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbContact = await _context.TbContacts.FindAsync(id);
            if (tbContact == null)
            {
                return NotFound();
            }
            return View(tbContact);
        }

        // POST: Admin/Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbContact = await _context.TbContacts.FindAsync(id);
            if (tbContact == null)
            {
                return NotFound();
            }

            // Bind fields manually to handle nullable bool
            tbContact.Name = Request.Form["Name"];
            tbContact.Email = Request.Form["Email"];
            tbContact.Phone = Request.Form["Phone"];
            tbContact.Subject = Request.Form["Subject"];
            tbContact.Message = Request.Form["Message"];
            tbContact.Response = Request.Form["Response"];

            // Handle nullable bool checkbox
            tbContact.IsRead = Request.Form["IsRead"].ToString() == "true";

            // Set response timestamp if response was added and not already set
            if (!string.IsNullOrEmpty(tbContact.Response) && tbContact.RespondedAt == null)
            {
                tbContact.RespondedAt = DateTime.Now;
            }

            // Set read timestamp if marked as read and not already set
            if (tbContact.IsRead == true && tbContact.ReadAt == null)
            {
                tbContact.ReadAt = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbContact);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Liên hệ đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbContactExists(tbContact.Id))
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
            return View(tbContact);
        }

        // GET: Admin/Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbContact = await _context.TbContacts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbContact == null)
            {
                return NotFound();
            }

            return View(tbContact);
        }

        // POST: Admin/Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbContact = await _context.TbContacts.FindAsync(id);
            if (tbContact != null)
            {
                _context.TbContacts.Remove(tbContact);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Liên hệ đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Contacts/SendResponse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendResponse(int id, string response)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var contact = await _context.TbContacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            contact.Response = response;
            contact.RespondedAt = DateTime.Now;
            contact.IsRead = true;
            if (contact.ReadAt == null)
                contact.ReadAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Phản hồi đã được gửi thành công";

            return RedirectToAction(nameof(Details), new { id });
        }

        private bool TbContactExists(int id)
        {
            return _context.TbContacts.Any(e => e.Id == id);
        }
    }
}
