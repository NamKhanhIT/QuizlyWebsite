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

        // POST: /contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("contact")]
        public async Task<IActionResult> Index(string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin");
                return View();
            }

            try
            {
                // In a real application, you would save this to a database or send an email
                // For now, we'll just log it
                _logger.LogInformation($"Contact form submitted: {name} ({email}) - {subject}: {message}");

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
