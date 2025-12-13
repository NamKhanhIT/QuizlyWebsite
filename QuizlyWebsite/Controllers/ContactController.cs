using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Controllers
{
    public class ContactController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<ContactController> _logger;

        public ContactController(QuizlyDbContext context, ILogger<ContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /contact
        [Route("contact")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("contact/history")]
        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            // Get user email from database
            var user = await _context.TbUsers.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var contacts = await _context.TbContacts
                .Where(c => c.Email == user.Email)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(contacts);
        }

        // POST: /contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("contact")]
        public async Task<IActionResult> Index(string name, string email, string? phone, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin bắt buộc");
                return View();
            }

            // Validate email format
            if (!email.Contains("@") || !email.Contains("."))
            {
                ModelState.AddModelError("email", "Email không hợp lệ");
                return View();
            }

            try
            {
                // Lưu thông tin liên hệ vào database
                var contact = new TbContact
                {
                    Name = name.Trim(),
                    Email = email.Trim(),
                    Phone = !string.IsNullOrWhiteSpace(phone) ? phone.Trim() : null,
                    Subject = subject.Trim(),
                    Message = message.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.TbContacts.Add(contact);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Contact form submitted and saved: {name} ({email}) - {subject}");

                TempData["Success"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing contact form: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại sau.");
            }

            return View();
        }
    }
}
