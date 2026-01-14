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
        public async Task<IActionResult> Create([Bind("Id,Username,Email,FullName,AvatarUrl,Role,CreatedAt,Address,PhoneNumber")] TbUser tbUser, string password)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Set PasswordHash tạm thời để pass validation (sẽ được set lại sau khi hash password)
            tbUser.PasswordHash = "";
            
            // Loại bỏ validation error cho PasswordHash (vì nó sẽ được set sau khi hash password)
            ModelState.Remove("PasswordHash");

            // Kiểm tra password không được rỗng
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("password", "Mật khẩu không được để trống");
            }

            // Kiểm tra Username không được rỗng
            if (string.IsNullOrWhiteSpace(tbUser.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập không được để trống");
            }
            else
            {
                // Kiểm tra Username không trùng
                var existingUsername = await _context.TbUsers
                    .FirstOrDefaultAsync(u => u.Username == tbUser.Username);
                if (existingUsername != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                }
            }

            // Kiểm tra Email không được rỗng
            if (string.IsNullOrWhiteSpace(tbUser.Email))
            {
                ModelState.AddModelError("Email", "Email không được để trống");
            }
            else
            {
                // Kiểm tra Email không trùng
                var existingEmail = await _context.TbUsers
                    .FirstOrDefaultAsync(u => u.Email == tbUser.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                }
            }

            // Nếu ModelState không hợp lệ, return View với model để hiển thị lỗi
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin đã nhập";
                return View(tbUser);
            }

            try
            {
                // Hash password bằng SHA256 và set vào PasswordHash
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                tbUser.PasswordHash = Convert.ToBase64String(hashedBytes);

                // Set giá trị mặc định
                tbUser.CreatedAt = DateTime.Now;
                
                // Set Role mặc định = "User" nếu không có
                if (string.IsNullOrWhiteSpace(tbUser.Role))
                {
                    tbUser.Role = "User";
                }

                // Lưu vào database
                _context.Add(tbUser);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Người dùng '{tbUser.Username}' đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // Xử lý lỗi database (ví dụ: unique constraint violation)
                ModelState.AddModelError("", $"Lỗi database: {dbEx.InnerException?.Message ?? dbEx.Message}");
                TempData["Error"] = "Không thể tạo người dùng. Vui lòng kiểm tra lại thông tin.";
                return View(tbUser);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi khác
                ModelState.AddModelError("", $"Lỗi khi tạo người dùng: {ex.Message}");
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return View(tbUser);
            }
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,FullName,AvatarUrl,Role,CreatedAt,Address,PhoneNumber")] TbUser tbUser, string password)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbUser.Id)
            {
                return NotFound();
            }

            // Load user hiện tại từ database
            var existingUser = await _context.TbUsers.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            ModelState.Remove("PasswordHash");
            
            ModelState.Remove("password");

            if (string.IsNullOrWhiteSpace(tbUser.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập không được để trống");
            }
            else
            {
                var existingUsername = await _context.TbUsers
                    .FirstOrDefaultAsync(u => u.Username == tbUser.Username && u.Id != id);
                if (existingUsername != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                }
            }

            if (string.IsNullOrWhiteSpace(tbUser.Email))
            {
                ModelState.AddModelError("Email", "Email không được để trống");
            }
            else
            {
                var existingEmail = await _context.TbUsers
                    .FirstOrDefaultAsync(u => u.Email == tbUser.Email && u.Id != id);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin đã nhập";
                return View(tbUser);
            }

            try
            {
                existingUser.Username = tbUser.Username;
                existingUser.Email = tbUser.Email;
                existingUser.FullName = tbUser.FullName;
                existingUser.AvatarUrl = tbUser.AvatarUrl;
                existingUser.Role = tbUser.Role;
                existingUser.Address = tbUser.Address;
                existingUser.PhoneNumber = tbUser.PhoneNumber;

                if (!string.IsNullOrWhiteSpace(password))
                {
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    existingUser.PasswordHash = Convert.ToBase64String(hashedBytes);
                }

                _context.Update(existingUser);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Người dùng '{existingUser.Username}' đã được cập nhật thành công";
                return RedirectToAction(nameof(Index));
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
            catch (DbUpdateException dbEx)
            {
                ModelState.AddModelError("", $"Lỗi database: {dbEx.InnerException?.Message ?? dbEx.Message}");
                TempData["Error"] = "Không thể cập nhật người dùng. Vui lòng kiểm tra lại thông tin.";
                return View(tbUser);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi cập nhật người dùng: {ex.Message}");
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return View(tbUser);
            }
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
                try
                {
                    var examsCreatedBy = await _context.TbExams
                        .Where(e => e.CreatedBy == id)
                        .ToListAsync();
                    foreach (var exam in examsCreatedBy)
                    {
                        exam.CreatedBy = null;
                    }

                    var examsApprovedBy = await _context.TbExams
                        .Where(e => e.ApprovedBy == id)
                        .ToListAsync();
                    foreach (var exam in examsApprovedBy)
                    {
                        exam.ApprovedBy = null;
                    }

                    var coursesCreatedBy = await _context.TbCourses
                        .Where(c => c.CreatedBy == id)
                        .ToListAsync();
                    foreach (var course in coursesCreatedBy)
                    {
                        course.CreatedBy = null;
                    }

                    var lessonsCreatedBy = await _context.TbLessons
                        .Where(l => l.CreatedBy == id)
                        .ToListAsync();
                    foreach (var lesson in lessonsCreatedBy)
                    {
                        lesson.CreatedBy = null;
                    }

                    var blogsCreatedBy = await _context.TbBlogs
                        .Where(b => b.CreatedAt == DateTime.Now)
                        .ToListAsync();
                    foreach (var blog in blogsCreatedBy)
                    {
                        blog.CreatedAt = null;
                    }

                    var examResults = await _context.TbExamResults
                        .Where(er => er.UserId == id)
                        .ToListAsync();
                    _context.TbExamResults.RemoveRange(examResults);

                    var examReviews = await _context.TbExamReviews
                        .Where(er => er.UserId == id)
                        .ToListAsync();
                    _context.TbExamReviews.RemoveRange(examReviews);

                    var examSessions = await _context.TbExamSessions
                        .Where(es => es.UserId == id)
                        .ToListAsync();
                    _context.TbExamSessions.RemoveRange(examSessions);

                    var lessonProgresses = await _context.TbLessonProgresses
                        .Where(lp => lp.UserId == id)
                        .ToListAsync();
                    _context.TbLessonProgresses.RemoveRange(lessonProgresses);

                    var payments = await _context.TbPayments
                        .Where(p => p.UserId == id)
                        .ToListAsync();
                    _context.TbPayments.RemoveRange(payments);

                    var userPurchases = await _context.TbUserPurchases
                        .Where(up => up.UserId == id)
                        .ToListAsync();
                    _context.TbUserPurchases.RemoveRange(userPurchases);

                    var userSubscriptions = await _context.TbUserSubscriptions
                        .Where(us => us.UserId == id)
                        .ToListAsync();
                    _context.TbUserSubscriptions.RemoveRange(userSubscriptions);

                    var userXp = await _context.TbUserXps
                        .Where(ux => ux.UserId == id)
                        .FirstOrDefaultAsync();
                    if (userXp != null)
                    {
                        _context.TbUserXps.Remove(userXp);
                    }

                    await _context.SaveChangesAsync();

                    _context.TbUsers.Remove(tbUser);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Người dùng đã được xóa thành công";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Lỗi khi xóa người dùng: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TbUserExists(int id)
        {
            return _context.TbUsers.Any(e => e.Id == id);
        }
    }
}
