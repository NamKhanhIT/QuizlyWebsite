using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExamsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public ExamsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: Admin/Exams
        public async Task<IActionResult> Index(string filter = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var query = _context.TbExams.Include(t => t.Subject).Include(t => t.CreatedByNavigation).AsQueryable();

            // Always show only approved exams in main management page
            query = query.Where(e => e.IsApproved == true);

            // Legacy filter support - but pending exams should go to approval page
            if (filter == "approved")
                query = query.Where(e => e.IsApproved == true);

            ViewData["Filter"] = "approved"; // Always show approved
            ViewData["PendingCount"] = await _context.TbExams.CountAsync(e => e.IsApproved == false);
            return View(await query.OrderByDescending(e => e.CreatedAt).ToListAsync());
        }

        // GET: Admin/Exams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbExam = await _context.TbExams
                .Include(t => t.Subject)
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.ApprovedByNavigation)
                .Include(t => t.TbQuestions)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbExam == null)
            {
                return NotFound();
            }

            return View(tbExam);
        }

        // GET: Admin/Exams/Create
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewData["CategoryId"] = new SelectList(_context.TbCategories.OrderBy(c => c.Title), "Id", "Title");
            ViewData["SubjectId"] = new SelectList(_context.TbSubjects.OrderBy(s => s.Title), "Id", "Title");
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email");
            return View();
        }

        // POST: Admin/Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TbExam tbExam)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Handle creation of new Category if provided
            int? createdCategoryId = null;
            var newCategoryName = Request.Form["NewCategoryName"].ToString();
            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                var cat = new TbCategory { Title = newCategoryName, CreatedAt = DateTime.Now };
                _context.TbCategories.Add(cat);
                await _context.SaveChangesAsync();
                createdCategoryId = cat.Id;
            }

            // Handle creation of new Subject if provided
            int? createdSubjectId = null;
            var newSubjectName = Request.Form["NewSubjectName"].ToString();
            if (!string.IsNullOrWhiteSpace(newSubjectName))
            {
                int subjectCategoryId = 0;
                if (int.TryParse(Request.Form["NewSubjectCategoryId"].ToString(), out var parsedCatId))
                    subjectCategoryId = parsedCatId;
                else if (createdCategoryId.HasValue)
                    subjectCategoryId = createdCategoryId.Value;
                else if (int.TryParse(Request.Form["CategoryId"].ToString(), out var postedCatId))
                    subjectCategoryId = postedCatId;

                if (subjectCategoryId > 0)
                {
                    var sub = new TbSubject { Title = newSubjectName, CategoryId = subjectCategoryId };
                    _context.TbSubjects.Add(sub);
                    await _context.SaveChangesAsync();
                    createdSubjectId = sub.Id;
                }
            }

            // Get SubjectId from form
            if (int.TryParse(Request.Form["SubjectId"].ToString(), out var subjectId) && subjectId > 0)
            {
                tbExam.SubjectId = subjectId;
            }
            else if (createdSubjectId.HasValue)
            {
                tbExam.SubjectId = createdSubjectId.Value;
            }

            // Xử lý loại đề thi
            var examType = Request.Form["examType"].ToString();
            tbExam.IsPremium = examType == "membership";
            tbExam.IsPaid = examType == "paid";

            // Xử lý giá nếu là đề thi trả phí
            if (examType == "paid" && !string.IsNullOrEmpty(Request.Form["Price"]))
            {
                if (decimal.TryParse(Request.Form["Price"], out decimal price))
                {
                    tbExam.Price = price;
                }
            }

            if (ModelState.IsValid && tbExam.SubjectId > 0)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                tbExam.CreatedBy = userId;
                tbExam.CreatedAt = DateTime.Now;
                tbExam.IsApproved = true; // Admin created exams are auto-approved

                _context.Add(tbExam);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đề thi đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            if (tbExam.SubjectId <= 0)
                ModelState.AddModelError("SubjectId", "Vui lòng chọn hoặc tạo môn học");
            
            ViewData["CategoryId"] = new SelectList(_context.TbCategories.OrderBy(c => c.Title), "Id", "Title");
            ViewData["SubjectId"] = new SelectList(_context.TbSubjects.OrderBy(s => s.Title), "Id", "Title", tbExam.SubjectId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbExam.CreatedBy);
            return View(tbExam);
        }

        // GET: Admin/Exams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbExam = await _context.TbExams.FindAsync(id);
            if (tbExam == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.TbCategories.OrderBy(c => c.Title), "Id", "Title");
            ViewData["SubjectId"] = new SelectList(_context.TbSubjects.OrderBy(s => s.Title), "Id", "Title", tbExam.SubjectId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbExam.CreatedBy);
            return View(tbExam);
        }

        // POST: Admin/Exams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TbExam tbExam)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id != tbExam.Id)
            {
                return NotFound();
            }

            // Get SubjectId from form
            if (int.TryParse(Request.Form["SubjectId"].ToString(), out var subjectId) && subjectId > 0)
            {
                tbExam.SubjectId = subjectId;
            }

            // Xử lý checkbox
            tbExam.IsApproved = Request.Form["IsApproved"].ToString() == "true";

            // Xử lý loại đề thi
            var examType = Request.Form["examType"].ToString();
            tbExam.IsPremium = examType == "membership";
            tbExam.IsPaid = examType == "paid";

            // Xử lý giá nếu là đề thi trả phí
            if (examType == "paid" && !string.IsNullOrEmpty(Request.Form["Price"]))
            {
                if (decimal.TryParse(Request.Form["Price"], out decimal price))
                {
                    tbExam.Price = price;
                }
            }
            else if (examType != "paid")
            {
                tbExam.Price = null; // Clear price for non-paid exams
            }

            if (ModelState.IsValid && tbExam.SubjectId > 0)
            {
                try
                {
                    _context.Update(tbExam);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đề thi đã được cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbExamExists(tbExam.Id))
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
            if (tbExam.SubjectId <= 0)
                ModelState.AddModelError("SubjectId", "Vui lòng chọn môn học");
            
            ViewData["CategoryId"] = new SelectList(_context.TbCategories.OrderBy(c => c.Title), "Id", "Title");
            ViewData["SubjectId"] = new SelectList(_context.TbSubjects.OrderBy(s => s.Title), "Id", "Title", tbExam.SubjectId);
            ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbExam.CreatedBy);
            return View(tbExam);
        }

        // GET: Admin/Exams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var tbExam = await _context.TbExams
                .Include(t => t.Subject)
                .Include(t => t.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tbExam == null)
            {
                return NotFound();
            }

            return View(tbExam);
        }

        // POST: Admin/Exams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbExam = await _context.TbExams.FindAsync(id);
            if (tbExam != null)
            {
                _context.TbExams.Remove(tbExam);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đề thi đã được xóa thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TbExamExists(int id)
        {
            return _context.TbExams.Any(e => e.Id == id);
        }
    }
}
