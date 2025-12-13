using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MenuController : Controller
    {
        private readonly QuizlyDbContext _context;

        public MenuController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/menu
        [Route("admin/menu")]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Lấy menu items từ database
            var menuItems = await _context.TbMenus
                .Where(m => m.Location == "Header" || m.Location == null)
                .OrderBy(m => m.Order)
                .ToListAsync();

            // Nếu chưa có menu items, tạo mặc định
            if (!menuItems.Any())
            {
                var defaultMenus = new List<TbMenu>
                {
                    new TbMenu { Title = "Trang chủ", Url = "/", Order = 1, Location = "Header", IsActive = true, CreatedAt = DateTime.Now },
                    new TbMenu { Title = "Đề thi", Url = "/quiz/list", Order = 2, Location = "Header", IsActive = true, CreatedAt = DateTime.Now },
                    new TbMenu { Title = "Khóa học", Url = "/courses", Order = 3, Location = "Header", IsActive = true, CreatedAt = DateTime.Now },
                    new TbMenu { Title = "Gói trả phí", Url = "/subscription/pricing", Order = 4, Location = "Header", IsActive = true, CreatedAt = DateTime.Now },
                    new TbMenu { Title = "Blogs/News", Url = "/blog", Order = 5, Location = "Header", IsActive = true, CreatedAt = DateTime.Now },
                    new TbMenu { Title = "Bảng xếp hạng", Url = "/leaderboard", Order = 6, Location = "Header", IsActive = true, CreatedAt = DateTime.Now },
                    new TbMenu { Title = "Liên hệ", Url = "/contact", Order = 7, Location = "Header", IsActive = true, CreatedAt = DateTime.Now }
                };

                await _context.TbMenus.AddRangeAsync(defaultMenus);
                await _context.SaveChangesAsync();
                menuItems = defaultMenus;
            }

            return View("~/Areas/Admin/Views/Home/Menu.cshtml", menuItems);
        }

        [HttpPost]
        [Route("admin/menu")]
        public async Task<IActionResult> UpdateMenu(IFormCollection form)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            try
            {
                // Xóa tất cả menu items cũ
                var oldMenus = await _context.TbMenus
                    .Where(m => m.Location == "Header" || m.Location == null)
                    .ToListAsync();
                _context.TbMenus.RemoveRange(oldMenus);

                // Lấy dữ liệu từ form
                var menuItems = new List<TbMenu>();
                var index = 0;

                while (form.ContainsKey($"menuItems[{index}].Title"))
                {
                    var title = form[$"menuItems[{index}].Title"].ToString();
                    var url = form[$"menuItems[{index}].Url"].ToString();
                    var orderStr = form[$"menuItems[{index}].Order"].ToString();

                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(url))
                    {
                        if (int.TryParse(orderStr, out int order))
                        {
                            menuItems.Add(new TbMenu
                            {
                                Title = title,
                                Url = url,
                                Order = order,
                                Location = "Header",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                    index++;
                }

                if (menuItems.Any())
                {
                    await _context.TbMenus.AddRangeAsync(menuItems);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Menu đã được cập nhật thành công";
                }
                else
                {
                    TempData["Error"] = "Vui lòng nhập ít nhất một menu item";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi cập nhật menu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
