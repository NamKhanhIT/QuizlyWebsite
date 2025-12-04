using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace QuizlyWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuizlyDbContext _context;

        public AccountController(QuizlyDbContext context)
        {
            _context = context;
        }

        // GET: /login
        [Route("/login")]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /login
        [Route("/login")]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập tên đăng nhập và mật khẩu");
                return View();
            }

            var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập không tồn tại");
                return View();
            }

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Mật khẩu không chính xác");
                return View();
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role ?? "User");

            // Redirect to admin if admin, else home
            if ((user.Role ?? "User") == "Admin")
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            
            return RedirectToAction("Index", "Home");
        }

        // GET: /register
        [Route("/register")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /register
        [Route("/register")]
        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword, string fullName)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng điền tất cả các trường bắt buộc");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu không trùng khớp");
                return View();
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
                return View();
            }

            // Check if username already exists
            var existingUser = await _context.TbUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Tên đăng nhập đã tồn tại");
                return View();
            }

            // Check if email already exists
            var existingEmail = await _context.TbUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("", "Email đã được đăng ký");
                return View();
            }

            // Create new user
            var newUser = new TbUser
            {
                Username = username,
                Email = email,
                FullName = fullName ?? username,
                PasswordHash = HashPassword(password),
                Role = "User",
                CreatedAt = DateTime.Now
            };

            _context.TbUsers.Add(newUser);
            await _context.SaveChangesAsync();

            // Auto login after registration
            HttpContext.Session.SetInt32("UserId", newUser.Id);
            HttpContext.Session.SetString("Username", newUser.Username);
            HttpContext.Session.SetString("Role", "User");

            return RedirectToAction("Index", "Home");
        }

        // GET: /logout
        [Route("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Helper methods
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }
    }
}
