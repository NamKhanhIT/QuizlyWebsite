using QuizlyWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using QuizlyWebsite.Services;

namespace QuizlyWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(QuizlyDbContext context, IEmailSender emailSender, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
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
            username = username?.Trim();
            password = password?.Trim();

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

            var isValid = VerifyPassword(password, user.PasswordHash);
            if (!isValid)
            {
                _logger.LogWarning("Login failed for user {Username}. Password verification failed.", username);
                ModelState.AddModelError("", "Mật khẩu không chính xác");
                return View();
            }
            
            _logger.LogInformation("Login successful for user {Username}", username);

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
            username = username?.Trim();
            email = email?.Trim();
            password = password?.Trim();
            confirmPassword = confirmPassword?.Trim();
            fullName = fullName?.Trim();

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

            var existingUser = await _context.TbUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Tên đăng nhập đã tồn tại");
                return View();
            }

            var existingEmail = await _context.TbUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("", "Email đã được đăng ký");
                return View();
            }

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

        // GET: /forgot-password
        [Route("/forgot-password")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /forgot-password
        [Route("/forgot-password")]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email");
                return View();
            }

            var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                var token = GeneratePasswordResetToken(user.Email);
                var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token = token }, Request.Scheme);

                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #2b8cee;'>Khôi Phục Mật Khẩu</h2>
                        <p>Xin chào,</p>
                        <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản của mình.</p>
                        <p>Vui lòng nhấp vào liên kết bên dưới để đặt lại mật khẩu:</p>
                        <p style='margin: 20px 0;'>
                            <a href='{resetLink}' style='background-color: #2b8cee; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Đặt Lại Mật Khẩu</a>
                        </p>
                        <p style='color: #ef4444; font-weight: bold;'> Lưu ý: Liên kết này chỉ có hiệu lực trong 15 phút.</p>
                        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                        <hr style='margin: 20px 0; border: none; border-top: 1px solid #e5e7eb;' />
                        <p style='color: #6b7280; font-size: 12px;'>Email này được gửi tự động, vui lòng không trả lời.</p>
                    </div>";

                await _emailSender.SendEmailAsync(user.Email, "Khôi Phục Mật Khẩu - Quizly", emailBody);
            }

            TempData["Success"] = "Nếu email của bạn tồn tại trong hệ thống, chúng tôi đã gửi link khôi phục mật khẩu đến email của bạn. Vui lòng kiểm tra hộp thư (kể cả thư mục spam).";
            return RedirectToAction("Login");
        }

        // GET: /reset-password
        [Route("/reset-password")]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "Link khôi phục mật khẩu không hợp lệ.";
                return RedirectToAction("Login");
            }

            // Validate token
            if (!ValidatePasswordResetToken(email, token))
            {
                TempData["Error"] = "Link khôi phục mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu link mới.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // POST: /reset-password
        [Route("/reset-password")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string token, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "Link khôi phục mật khẩu không hợp lệ.";
                return RedirectToAction("Login");
            }

            if (!ValidatePasswordResetToken(email, token))
            {
                TempData["Error"] = "Link khôi phục mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu link mới.";
                return RedirectToAction("ForgotPassword");
            }

            password = password?.Trim();
            confirmPassword = confirmPassword?.Trim();

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập mật khẩu mới");
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu và xác nhận mật khẩu không trùng khớp");
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Login");
            }

            var newPasswordHash = HashPassword(password);
            
            _logger.LogInformation("Resetting password for user {Email}. New hash length: {HashLength}", email, newPasswordHash?.Length ?? 0);
            user.PasswordHash = newPasswordHash;
            
            try
            {
                var saved = await _context.SaveChangesAsync();
                _logger.LogInformation("Password reset saved. Changes: {Saved}", saved);
                
                // Verify: reload user từ DB để đảm bảo password đã được lưu
                _context.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                var verifyUser = await _context.TbUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
                if (verifyUser != null && !string.IsNullOrEmpty(verifyUser.PasswordHash) && verifyUser.PasswordHash == newPasswordHash)
                {
                    _logger.LogInformation("Password reset verified successfully for user {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Password reset verification failed for user {Email}. Hash in DB: {DbHash}, Expected: {ExpectedHash}", 
                        email, verifyUser?.PasswordHash?.Substring(0, Math.Min(10, verifyUser.PasswordHash?.Length ?? 0)) ?? "null", 
                        newPasswordHash?.Substring(0, Math.Min(10, newPasswordHash?.Length ?? 0)) ?? "null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving password reset for user {Email}", email);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật mật khẩu. Vui lòng thử lại.");
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            TempData["Success"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.";
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            var normalizedPassword = password.Trim();

            if (string.IsNullOrEmpty(normalizedPassword))
                throw new ArgumentException("Password cannot be empty or whitespace only", nameof(password));

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedPassword));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            var hashOfInput = HashPassword(password);
            
            return hashOfInput == hash;
        }

        private string GeneratePasswordResetToken(string email)
        {
            var secretKey = _configuration["PasswordReset:SecretKey"] ?? "QuizlyPasswordResetSecretKey2024!@#$%^&*()";
            var expiryMinutes = _configuration.GetValue<int>("PasswordReset:TokenExpiryMinutes", 15);
            var expiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes);
            var payload = $"{email}|{expiryTime:O}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var token = $"{payload}|{Convert.ToBase64String(signature)}";
                
                return Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            }
        }
        private bool ValidatePasswordResetToken(string email, string token)
        {
            try
            {
                var secretKey = _configuration["PasswordReset:SecretKey"] ?? "QuizlyPasswordResetSecretKey2024!@#$%^&*()";
                var tokenBytes = Base64UrlDecode(token);
                var tokenString = Encoding.UTF8.GetString(tokenBytes);

                var parts = tokenString.Split('|');
                if (parts.Length != 3)
                    return false;

                var payload = $"{parts[0]}|{parts[1]}";
                var signature = Convert.FromBase64String(parts[2]);

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
                {
                    var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                    if (!signature.SequenceEqual(expectedSignature))
                        return false;
                }

                if (parts[0] != email)
                    return false;

                if (DateTime.TryParse(parts[1], out var expiryTime))
                {
                    if (expiryTime < DateTime.UtcNow)
                        return false;
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        private byte[] Base64UrlDecode(string input)
        {
            var base64 = input
                .Replace('-', '+')
                .Replace('_', '/');
            
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            
            return Convert.FromBase64String(base64);
        }
    }
}
