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

            query = query.Where(e => e.IsApproved == true);

            if (filter == "approved")
                query = query.Where(e => e.IsApproved == true);

            ViewData["Filter"] = "approved"; // Always show approved
            // Chỉ đếm đề thi chờ duyệt (IsApproved == false/null) và chưa bị từ chối (RejectionReason == null)
            ViewData["PendingCount"] = await _context.TbExams
                .CountAsync(e => (e.IsApproved == false || e.IsApproved == null) && e.RejectionReason == null);
            return View(await query.OrderByDescending(e => e.CreatedAt).ToListAsync());
        }

        // GET: Admin/Exams/Details/5
        public async Task<IActionResult> Details(int? id, bool? fromApproval)
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

            ViewData["FromApproval"] = fromApproval ?? false;

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
        public async Task<IActionResult> Create(IFormFile? ImageFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbExam = new TbExam();

            // Bind basic fields from form
            tbExam.Title = Request.Form["Title"];
            tbExam.Difficulty = Request.Form["Difficulty"];
            if (int.TryParse(Request.Form["Duration"], out int duration))
                tbExam.Duration = duration;
            if (int.TryParse(Request.Form["QuestionCount"], out int questionCount))
                tbExam.QuestionCount = questionCount;

            int? createdCategoryId = null;
            var newCategoryName = Request.Form["NewCategoryName"].ToString();
            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                var cat = new TbCategory { Title = newCategoryName, CreatedAt = DateTime.Now };
                _context.TbCategories.Add(cat);
                await _context.SaveChangesAsync();
                createdCategoryId = cat.Id;
            }

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

            if (createdSubjectId.HasValue)
            {
                tbExam.SubjectId = createdSubjectId.Value;
            }
            else if (int.TryParse(Request.Form["SubjectId"], out var subjectId))
            {
                tbExam.SubjectId = subjectId;
            }

            var examType = Request.Form["examType"].ToString();
            tbExam.IsPremium = examType == "membership";
            tbExam.IsPaid = examType == "paid";

            if (examType == "paid" && !string.IsNullOrEmpty(Request.Form["Price"]))
            {
                if (decimal.TryParse(Request.Form["Price"], out decimal price))
                {
                    tbExam.Price = price;
                }
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(ImageFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận file ảnh có định dạng JPG, PNG, GIF.");
                }
                else
                {
                    if (ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Kích thước file không được vượt quá 5MB.");
                    }
                    else
                    {
                        try
                        {
                            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exams");
                            if (!Directory.Exists(uploadsDir))
                            {
                                Directory.CreateDirectory(uploadsDir);
                            }

                            // Generate unique filename
                            var fileName = $"{Guid.NewGuid()}{extension}";
                            var filePath = Path.Combine(uploadsDir, fileName);

                            // Save file
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await ImageFile.CopyToAsync(stream);
                            }

                            // Set image URL
                            tbExam.ImageUrl = $"/uploads/exams/{fileName}";
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", "Có lỗi xảy ra khi tải lên ảnh.");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                tbExam.CreatedBy = userId;
                tbExam.CreatedAt = DateTime.Now;
                tbExam.IsApproved = true; 
                _context.Add(tbExam);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đề thi đã được tạo thành công";
                return RedirectToAction(nameof(Index));
            }
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
        public async Task<IActionResult> Edit(int id, IFormFile? ImageFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var tbExam = await _context.TbExams.FindAsync(id);
            if (tbExam == null)
            {
                return NotFound();
            }

            System.Diagnostics.Debug.WriteLine($"Edit exam {id}");

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(ImageFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận file ảnh có định dạng JPG, PNG, GIF.");
                    ViewData["CategoryId"] = new SelectList(_context.TbCategories.OrderBy(c => c.Title), "Id", "Title");
                    ViewData["SubjectId"] = new SelectList(_context.TbSubjects.OrderBy(s => s.Title), "Id", "Title", tbExam.SubjectId);
                    ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbExam.CreatedBy);
                    return View(tbExam);
                }

                if (ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Kích thước file không được vượt quá 5MB.");
                    ViewData["CategoryId"] = new SelectList(_context.TbCategories.OrderBy(c => c.Title), "Id", "Title");
                    ViewData["SubjectId"] = new SelectList(_context.TbSubjects.OrderBy(s => s.Title), "Id", "Title", tbExam.SubjectId);
                    ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbExam.CreatedBy);
                    return View(tbExam);
                }

                try
                {
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exams");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    tbExam.ImageUrl = $"/uploads/exams/{fileName}";
                    System.Diagnostics.Debug.WriteLine($"Uploaded image: {tbExam.ImageUrl}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error uploading image: {ex.Message}");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tải lên ảnh.");
                    ViewData["CategoryId"] = new SelectList(_context.TbCategories.OrderBy(c => c.Title), "Id", "Title");
                    ViewData["SubjectId"] = new SelectList(_context.TbSubjects.OrderBy(s => s.Title), "Id", "Title", tbExam.SubjectId);
                    ViewData["CreatedBy"] = new SelectList(_context.TbUsers, "Id", "Email", tbExam.CreatedBy);
                    return View(tbExam);
                }
            }

            int? createdCategoryId = null;
            var newCategoryName = Request.Form["NewCategoryName"].ToString();
            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                var cat = new TbCategory { Title = newCategoryName, CreatedAt = DateTime.Now };
                _context.TbCategories.Add(cat);
                await _context.SaveChangesAsync();
                createdCategoryId = cat.Id;
                System.Diagnostics.Debug.WriteLine($"Created new category {cat.Id}: {cat.Title}");
            }
            int finalSubjectId = tbExam.SubjectId;
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
                    finalSubjectId = sub.Id;
                    System.Diagnostics.Debug.WriteLine($"Created new subject {sub.Id}: {sub.Title}");
                }
            }
            else if (int.TryParse(Request.Form["SubjectId"].ToString(), out var selectedSubjectId))
            {
                finalSubjectId = selectedSubjectId;
            }

            tbExam.SubjectId = finalSubjectId;
            tbExam.Title = Request.Form["Title"].ToString();
            tbExam.Difficulty = Request.Form["Difficulty"].ToString();
            tbExam.Duration = int.Parse(Request.Form["Duration"].ToString());
            tbExam.QuestionCount = int.Parse(Request.Form["QuestionCount"].ToString());

            var imageUrl = Request.Form["ImageUrl"].ToString();
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                tbExam.ImageUrl = imageUrl;
            }
            tbExam.IsApproved = Request.Form["IsApproved"].ToString() == "true";

            var examType = Request.Form["examType"].ToString();
            if (examType == "paid")
            {
                tbExam.IsPaid = true;
                if (!string.IsNullOrEmpty(Request.Form["Price"]))
                {
                    if (decimal.TryParse(Request.Form["Price"], out decimal price) && price > 0)
                    {
                        tbExam.Price = price;
                    }
                }
            }
            else
            {
                tbExam.IsPaid = false;
                tbExam.Price = 0;
            }
            tbExam.IsPremium = false;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Saving exam {id}: SubjectId={tbExam.SubjectId}, Title={tbExam.Title}, IsApproved={tbExam.IsApproved}, IsPremium={tbExam.IsPremium}, IsPaid={tbExam.IsPaid}");
                _context.Update(tbExam);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đề thi đã được cập nhật thành công";
                return RedirectToAction(nameof(Index));
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving exam {id}: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật đề thi.");
            }

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
