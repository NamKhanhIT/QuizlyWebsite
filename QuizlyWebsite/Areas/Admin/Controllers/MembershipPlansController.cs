using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using Microsoft.Extensions.Logging;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MembershipPlansController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<MembershipPlansController> _logger;

        public MembershipPlansController(QuizlyDbContext context, ILogger<MembershipPlansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("IsAdmin() returned false: Session Role is null or empty");
                return false;
            }
            var isAdmin = role == "Admin";
            if (!isAdmin)
            {
                _logger.LogWarning("IsAdmin() returned false: Role is '{Role}', expected 'Admin'", role);
            }
            return isAdmin;
        }

        // GET: Admin/MembershipPlans
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            return View(await _context.TbMembershipPlans.OrderBy(m => m.Id).ToListAsync());
        }

        // GET: Admin/MembershipPlans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbMembershipPlan = await _context.TbMembershipPlans
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbMembershipPlan == null)
            {
                return NotFound();
            }

            return View(tbMembershipPlan);
        }

        // GET: Admin/MembershipPlans/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            return View();
        }

        // POST: Admin/MembershipPlans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Price,DurationDays")] TbMembershipPlan tbMembershipPlan)
        {
            _logger.LogInformation("POST Create MembershipPlan called");

            if (!IsAdmin())
            {
                _logger.LogWarning("POST Create: User is not admin, returning 403");
                return StatusCode(403, "Không có quyền truy cập");
            }

            // Loại bỏ validation cho UserSubscriptions (navigation property, không cần khi tạo)
            ModelState.Remove("UserSubscriptions");

            // Validation
            if (string.IsNullOrWhiteSpace(tbMembershipPlan.Title))
            {
                ModelState.AddModelError("Title", "Tiêu đề không được để trống");
            }

            if (tbMembershipPlan.Price == null || tbMembershipPlan.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá phải lớn hơn 0");
            }

            if (tbMembershipPlan.DurationDays == null || tbMembershipPlan.DurationDays <= 0)
            {
                ModelState.AddModelError("DurationDays", "Thời hạn phải lớn hơn 0");
            }

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("POST Create: ModelState is invalid");
                foreach (var error in ModelState)
                {
                    foreach (var errorMessage in error.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error - {Key}: {Message}", error.Key, errorMessage.ErrorMessage);
                    }
                }
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin đã nhập";
                return View(tbMembershipPlan);
            }

            try
            {
                _logger.LogInformation("Creating MembershipPlan: Title={Title}, Price={Price}, DurationDays={DurationDays}", 
                    tbMembershipPlan.Title, tbMembershipPlan.Price, tbMembershipPlan.DurationDays);

                // Khởi tạo UserSubscriptions để tránh null reference
                tbMembershipPlan.UserSubscriptions = new List<TbUserSubscription>();

                _context.Add(tbMembershipPlan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("MembershipPlan created successfully with ID: {Id}", tbMembershipPlan.Id);
                TempData["Success"] = $"Gói thành viên '{tbMembershipPlan.Title}' đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error when creating MembershipPlan");
                ModelState.AddModelError("", $"Lỗi database: {dbEx.InnerException?.Message ?? dbEx.Message}");
                TempData["Error"] = "Không thể tạo gói thành viên. Vui lòng kiểm tra lại thông tin.";
                return View(tbMembershipPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MembershipPlan");
                ModelState.AddModelError("", $"Lỗi khi tạo gói thành viên: {ex.Message}");
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return View(tbMembershipPlan);
            }
        }

        // GET: Admin/MembershipPlans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbMembershipPlan = await _context.TbMembershipPlans.FindAsync(id);
            if (tbMembershipPlan == null)
            {
                return NotFound();
            }
            return View(tbMembershipPlan);
        }

        // POST: Admin/MembershipPlans/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Price,DurationDays")] TbMembershipPlan tbMembershipPlan)
        {
            _logger.LogInformation("POST Edit MembershipPlan called with ID: {Id}", id);

            if (!IsAdmin())
            {
                _logger.LogWarning("POST Edit: User is not admin, returning 403");
                return StatusCode(403, "Không có quyền truy cập");
            }

            if (id != tbMembershipPlan.Id)
            {
                _logger.LogWarning("POST Edit: ID mismatch. Route ID: {RouteId}, Model ID: {ModelId}", id, tbMembershipPlan.Id);
                return NotFound();
            }

            // Loại bỏ validation cho UserSubscriptions (navigation property, không cần khi sửa)
            ModelState.Remove("UserSubscriptions");

            // Load entity từ database
            var existingPlan = await _context.TbMembershipPlans.FindAsync(id);
            if (existingPlan == null)
            {
                _logger.LogWarning("POST Edit: MembershipPlan with ID {Id} not found", id);
                return NotFound();
            }

            // Validation
            if (string.IsNullOrWhiteSpace(tbMembershipPlan.Title))
            {
                ModelState.AddModelError("Title", "Tiêu đề không được để trống");
            }

            if (tbMembershipPlan.Price == null || tbMembershipPlan.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá phải lớn hơn 0");
            }

            if (tbMembershipPlan.DurationDays == null || tbMembershipPlan.DurationDays <= 0)
            {
                ModelState.AddModelError("DurationDays", "Thời hạn phải lớn hơn 0");
            }

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("POST Edit: ModelState is invalid");
                foreach (var error in ModelState)
                {
                    foreach (var errorMessage in error.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error - {Key}: {Message}", error.Key, errorMessage.ErrorMessage);
                    }
                }
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin đã nhập";
                return View(tbMembershipPlan);
            }

            try
            {
                _logger.LogInformation("Updating MembershipPlan ID {Id}: Title={Title}, Price={Price}, DurationDays={DurationDays}", 
                    id, tbMembershipPlan.Title, tbMembershipPlan.Price, tbMembershipPlan.DurationDays);

                // Cập nhật từng field
                existingPlan.Title = tbMembershipPlan.Title;
                existingPlan.Price = tbMembershipPlan.Price;
                existingPlan.DurationDays = tbMembershipPlan.DurationDays;

                _context.Update(existingPlan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("MembershipPlan ID {Id} updated successfully", id);
                TempData["Success"] = $"Gói thành viên '{existingPlan.Title}' đã được cập nhật thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency exception when updating MembershipPlan ID {Id}", id);
                if (!TbMembershipPlanExists(tbMembershipPlan.Id))
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
                _logger.LogError(dbEx, "Database error when updating MembershipPlan ID {Id}", id);
                ModelState.AddModelError("", $"Lỗi database: {dbEx.InnerException?.Message ?? dbEx.Message}");
                TempData["Error"] = "Không thể cập nhật gói thành viên. Vui lòng kiểm tra lại thông tin.";
                return View(tbMembershipPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating MembershipPlan ID {Id}", id);
                ModelState.AddModelError("", $"Lỗi khi cập nhật gói thành viên: {ex.Message}");
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return View(tbMembershipPlan);
            }
        }

        // GET: Admin/MembershipPlans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbMembershipPlan = await _context.TbMembershipPlans
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbMembershipPlan == null)
            {
                return NotFound();
            }

            return View(tbMembershipPlan);
        }

        // POST: Admin/MembershipPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("POST Delete MembershipPlan called with ID: {Id}", id);

            if (!IsAdmin())
            {
                _logger.LogWarning("POST Delete: User is not admin, returning 403");
                return StatusCode(403, "Không có quyền truy cập");
            }

            var tbMembershipPlan = await _context.TbMembershipPlans.FindAsync(id);
            if (tbMembershipPlan == null)
            {
                _logger.LogWarning("POST Delete: MembershipPlan with ID {Id} not found", id);
                TempData["Error"] = "Không tìm thấy gói thành viên cần xóa";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Kiểm tra xem có UserSubscriptions đang sử dụng plan này không
                var hasSubscriptions = await _context.TbUserSubscriptions
                    .AnyAsync(us => us.PlanId == id);
                
                if (hasSubscriptions)
                {
                    _logger.LogWarning("Cannot delete MembershipPlan ID {Id}: Has active subscriptions", id);
                    TempData["Error"] = "Không thể xóa gói thành viên này vì đang có người dùng đăng ký. Vui lòng xóa các đăng ký trước.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Deleting MembershipPlan ID {Id}: Title={Title}", id, tbMembershipPlan.Title);

                _context.TbMembershipPlans.Remove(tbMembershipPlan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("MembershipPlan ID {Id} deleted successfully", id);
                TempData["Success"] = $"Gói thành viên '{tbMembershipPlan.Title}' đã được xóa thành công";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error when deleting MembershipPlan ID {Id}", id);
                TempData["Error"] = $"Lỗi database: {dbEx.InnerException?.Message ?? dbEx.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting MembershipPlan ID {Id}", id);
                TempData["Error"] = $"Lỗi khi xóa gói thành viên: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TbMembershipPlanExists(int id)
        {
            return _context.TbMembershipPlans.Any(e => e.Id == id);
        }
    }
}
