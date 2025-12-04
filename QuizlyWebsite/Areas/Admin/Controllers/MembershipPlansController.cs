using System;
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

        // GET: /admin/membership-plans
        [Route("admin/membership-plans")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var plans = await _context.TbMembershipPlans
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var total = await _context.TbMembershipPlans.CountAsync();
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/MembershipPlans.cshtml", plans);
        }

        // GET: /admin/membership-plan-form or /admin/membership-plan-form/{id}
        [Route("admin/membership-plan-form")]
        [Route("admin/membership-plan-form/{id}")]
        public async Task<IActionResult> Form(int? id)
        {
            if (id.HasValue)
            {
                var plan = await _context.TbMembershipPlans.FindAsync(id.Value);
                if (plan == null) return NotFound();
                return View("~/Areas/Admin/Views/Home/MembershipPlanForm.cshtml", plan);
            }

            return View("~/Areas/Admin/Views/Home/MembershipPlanForm.cshtml", new TbMembershipPlan());
        }

        // POST: /admin/membership-plan-form or /admin/membership-plan-form/{id}
        [HttpPost]
        [Route("admin/membership-plan-form")]
        [Route("admin/membership-plan-form/{id}")]
        public async Task<IActionResult> FormPost(int? id, string title, decimal price, int durationDays)
        {
            if (!ModelState.IsValid)
            {
                var model = id.HasValue ? await _context.TbMembershipPlans.FindAsync(id) : new TbMembershipPlan();
                return View("~/Areas/Admin/Views/Home/MembershipPlanForm.cshtml", model);
            }

            try
            {
                if (id.HasValue && id > 0)
                {
                    var plan = await _context.TbMembershipPlans.FindAsync(id.Value);
                    if (plan == null) return NotFound();
                    plan.Title = title;
                    plan.Price = price;
                    plan.DurationDays = durationDays;

                    _context.TbMembershipPlans.Update(plan);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Gói hội viên đã được cập nhật";
                }
                else
                {
                    var plan = new TbMembershipPlan
                    {
                        Title = title,
                        Price = price,
                        DurationDays = durationDays
                    };

                    await _context.TbMembershipPlans.AddAsync(plan);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Gói hội viên mới đã được tạo";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: " + ex.Message);
                var model = id.HasValue ? await _context.TbMembershipPlans.FindAsync(id) : new TbMembershipPlan();
                return View("~/Areas/Admin/Views/Home/MembershipPlanForm.cshtml", model);
            }
        }

        // POST: /admin/delete-membership-plan (used by AJAX)
        [HttpPost]
        [Route("admin/delete-membership-plan")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest req)
        {
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var plan = await _context.TbMembershipPlans.FindAsync(req.Id);
            if (plan == null) return Json(new { success = false, message = "Gói không tồn tại" });

            _context.TbMembershipPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class DeleteRequest { public int Id { get; set; } }
    }
}
