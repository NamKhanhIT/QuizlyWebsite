using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MembershipPlansController : Controller
    {
        private readonly QuizlyDbContext _context;

        public MembershipPlansController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
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
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (ModelState.IsValid)
            {
                _context.Add(tbMembershipPlan);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Gói thành viên đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            return View(tbMembershipPlan);
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
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbMembershipPlan.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbMembershipPlan);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Gói thành viên đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbMembershipPlanExists(tbMembershipPlan.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tbMembershipPlan);
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
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbMembershipPlan = await _context.TbMembershipPlans.FindAsync(id);
            if (tbMembershipPlan != null)
            {
                _context.TbMembershipPlans.Remove(tbMembershipPlan);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Gói thành viên đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TbMembershipPlanExists(int id)
        {
            return _context.TbMembershipPlans.Any(e => e.Id == id);
        }
    }
}
