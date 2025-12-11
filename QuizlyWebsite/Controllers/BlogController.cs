using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Controllers
{
    public class BlogController : Controller
    {
        private readonly QuizlyDbContext _context;

        public BlogController(QuizlyDbContext context)
        {
            _context = context;
        }

        // GET: /blog or /news
        [Route("blog")]
        [Route("news")]
        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            var query = _context.TbBlogs
                .Where(b => b.IsPublished == true)
                .Include(b => b.Author)
                .OrderByDescending(b => b.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Title.Contains(search) || 
                                        (b.Summary != null && b.Summary.Contains(search)) ||
                                        b.Content.Contains(search));
            }

            var total = await query.CountAsync();
            var blogs = await query
                .Skip((page - 1) * 9)
                .Take(9)
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (total + 8) / 9;
            ViewData["Search"] = search;

            return View(blogs);
        }

        // GET: /blog/{id} or /blog/{slug}
        [Route("blog/{id:int}")]
        [Route("blog/{slug}")]
        public async Task<IActionResult> Detail(int? id, string? slug)
        {
            TbBlog? blog = null;

            if (id.HasValue)
            {
                blog = await _context.TbBlogs
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.Id == id.Value && b.IsPublished == true);
            }
            else if (!string.IsNullOrWhiteSpace(slug))
            {
                blog = await _context.TbBlogs
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished == true);
            }

            if (blog == null)
                return NotFound();

            // Get related blogs
            var relatedBlogs = await _context.TbBlogs
                .Where(b => b.IsPublished == true && b.Id != blog.Id)
                .Include(b => b.Author)
                .OrderByDescending(b => b.CreatedAt)
                .Take(3)
                .ToListAsync();

            ViewBag.RelatedBlogs = relatedBlogs;

            return View(blog);
        }
    }
}
