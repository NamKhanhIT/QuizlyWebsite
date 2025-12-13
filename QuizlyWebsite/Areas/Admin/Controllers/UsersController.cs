using System;
using System.Security.Cryptography;
using System.Text;
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

        // GET: /admin/users
        [Route("admin/users")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var users = await _context.TbUsers
                .OrderByDescending(u => u.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(u => u.Id)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbUsers.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/Users.cshtml", users);
        }

        // GET: /admin/user-form or /admin/user-form/{id}
        [Route("admin/user-form")]
        [Route("admin/user-form/{id}")]
        public async Task<IActionResult> Form(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id.HasValue)
            {
                var user = await _context.TbUsers.FindAsync(id.Value);
                if (user == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/UserForm.cshtml", user);
            }

            return View("~/Areas/Admin/Views/Home/UserForm.cshtml", new TbUser());
        }

        // POST: /admin/user-form or /admin/user-form/{id}
        [HttpPost]
        [Route("admin/user-form")]
        [Route("admin/user-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, string username, string email, string? fullName, string? phoneNumber, string? address, string? avatarUrl, string? password)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (string.IsNullOrWhiteSpace(username))
                ModelState.AddModelError(nameof(username), "Tên đăng nhập không được để trống");

            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError(nameof(email), "Email không được để trống");

            // Check if username already exists (for new users)
            if (!id.HasValue || id == 0)
            {
                if (string.IsNullOrWhiteSpace(password))
                    ModelState.AddModelError(nameof(password), "Mật khẩu không được để trống");

                var existingUser = await _context.TbUsers.FirstOrDefaultAsync(u => u.Username == username);
                if (existingUser != null)
                    ModelState.AddModelError(nameof(username), "Tên đăng nhập đã tồn tại");
            }

            // Check if email already exists
            var existingEmail = await _context.TbUsers.FirstOrDefaultAsync(u => u.Email == email && (!id.HasValue || u.Id != id.Value));
            if (existingEmail != null)
                ModelState.AddModelError(nameof(email), "Email đã tồn tại");

            if (!ModelState.IsValid)
            {
                var model = id.HasValue ? await _context.TbUsers.FindAsync(id) : new TbUser();
                return View("~/Areas/Admin/Views/Home/UserForm.cshtml", model);
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var user = await _context.TbUsers.FindAsync(id.Value);
                    if (user == null) return NotFound();
                    user.Email = email;
                    user.FullName = fullName;
                    user.PhoneNumber = phoneNumber;
                    user.Address = address;
                    user.AvatarUrl = avatarUrl;

                    _context.TbUsers.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Người dùng đã được cập nhật thành công";
                }
                else
                {
                    // Hash password using SHA256 (same as AccountController)
                    string passwordHash = HashPassword(password ?? "");

                    var user = new TbUser
                    {
                        Username = username,
                        Email = email,
                        FullName = fullName,
                        PhoneNumber = phoneNumber,
                        Address = address,
                        AvatarUrl = avatarUrl,
                        PasswordHash = passwordHash,
                        Role = "User",
                        CreatedAt = DateTime.Now
                    };

                    await _context.TbUsers.AddAsync(user);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Người dùng mới đã được tạo thành công";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                var model = id.HasValue ? await _context.TbUsers.FindAsync(id) : new TbUser();
                return View("~/Areas/Admin/Views/Home/UserForm.cshtml", model);
            }
        }

        // GET: /admin/user-permissions
        [Route("admin/user-permissions")]
        public async Task<IActionResult> UserPermissions()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var users = await _context.TbUsers
                .OrderByDescending(u => u.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(u => u.Id)
                .ToListAsync();
            return View("~/Areas/Admin/Views/Home/UserPermissions.cshtml", users);
        }

        // POST: /admin/users/{id}/permissions - Update user role
        [HttpPost]
        [Route("admin/users/{id}/permissions")]
        public async Task<IActionResult> UserPermissionsPost(int id, string role)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            var user = await _context.TbUsers.FindAsync(id);
            if (user == null)
                return Json(new { success = false, message = "Người dùng không tồn tại" });

            if (string.IsNullOrWhiteSpace(role) || (role != "User" && role != "Admin"))
                return Json(new { success = false, message = "Vai trò không hợp lệ. Chỉ có User và Admin." });

            try
            {
                user.Role = role;
                _context.TbUsers.Update(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Vai trò đã được cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật vai trò: " + ex.Message });
            }
        }

        // POST: /admin/delete-user (used by AJAX)
        [HttpPost]
        [Route("admin/delete-user")]
        public async Task<IActionResult> Delete([FromBody] DeleteUserRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(req.Reason))
                return Json(new { success = false, message = "Vui lòng nhập lý do xóa" });

            var user = await _context.TbUsers.FindAsync(req.Id);
            if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại" });

            // Không cho phép xóa chính mình
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (user.Id == currentUserId)
                return Json(new { success = false, message = "Không thể xóa chính mình" });

            // Không cho phép xóa admin khác
            if (user.Role == "Admin")
                return Json(new { success = false, message = "Không thể xóa tài khoản Admin" });
            
            try
            {
                _context.TbUsers.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Người dùng đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa người dùng: " + ex.Message });
            }
        }

        public class DeleteRequest { public int Id { get; set; } }
        public class DeleteUserRequest { public int Id { get; set; } public string Reason { get; set; } = string.Empty; }

        // Helper method for password hashing (same as AccountController)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
