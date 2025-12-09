using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly QuizlyDbContext _context;

        public HomeController(ILogger<HomeController> logger, QuizlyDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy testimonials từ database - top 3 reviews có rating cao nhất
            var testimonials = _context.TbExamReviews
                .Where(r => r.Rating >= 4 && !string.IsNullOrEmpty(r.Comment))
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedAt)
                .Take(3)
                .Join(_context.TbUsers,
                    review => review.UserId,
                    user => user.Id,
                    (review, user) => new
                    {
                        review.Id,
                        user.Username,
                        user.FullName,
                        user.Role,
                        user.AvatarUrl,
                        review.Rating,
                        review.Comment,
                        review.CreatedAt
                    })
                .ToList()
                .Select(x => new TestimonialViewModel
                {
                    Id = x.Id,
                    UserName = x.Username ?? "Anonymous",
                    FullName = x.FullName ?? "User",
                    UserRole = GetUserRole(x.Role ?? "User"),
                    AvatarUrl = x.AvatarUrl ?? "",
                    Rating = x.Rating ?? 5,
                    Comment = x.Comment ?? "",
                    CreatedAt = x.CreatedAt ?? DateTime.Now
                })
                .ToList();

            return View(testimonials);
        }

        private static string GetUserRole(string role)
        {
            return role switch
            {
                "Admin" => "Quản Trị Viên",
                "User" => "Người Dùng",
                _ => "Người Dùng"
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
