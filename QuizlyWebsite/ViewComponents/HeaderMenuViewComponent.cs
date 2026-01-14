using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.ViewComponents
{
    public class HeaderMenuViewComponent : ViewComponent
    {
        private readonly QuizlyDbContext _context;

        public HeaderMenuViewComponent(QuizlyDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var menus = await _context.TbMenus
                .Where(m => m.ParentId == null 
                    && m.IsActive == true 
                    && m.Location == "HEADER")
                .Include(m => m.InverseParent.Where(child => child.IsActive == true))
                .OrderBy(m => m.Order ?? 0)
                .ThenBy(m => m.Id)
                .ToListAsync();

            return View(menus);
        }
    }
}
