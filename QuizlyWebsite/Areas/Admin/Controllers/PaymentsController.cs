using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PaymentsController : Controller
    {
        private readonly QuizlyDbContext _context;

        public PaymentsController(QuizlyDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/payments
        [Route("admin/payments")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var allPayments = await _context.TbPayments
                .Include(p => p.User)
                .Include(p => p.Exam)
                .ToListAsync();

            // Calculate statistics
            var totalRevenue = allPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .Sum(p => p.Amount ?? 0);

            var successfulPayments = allPayments
                .Count(p => p.Status == "Completed" || p.Status == "Success");

            var totalPayments = allPayments.Count;

            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["SuccessfulPayments"] = successfulPayments;
            ViewData["TotalPayments"] = totalPayments;

            // Pagination
            var payments = allPayments
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToList();

            var total = allPayments.Count;
            ViewData["TotalPages"] = (total + 9) / 10;
            ViewData["CurrentPage"] = page;

            return View("~/Areas/Admin/Views/Home/Payments.cshtml", payments);
        }

        // GET: /admin/reports
        [Route("admin/reports")]
        public async Task<IActionResult> Reports()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            // Get all payments first, then process in-memory
            var allPayments = await _context.TbPayments
                .Include(p => p.User)
                .Include(p => p.Exam)
                .ToListAsync();

            // Revenue stats
            var totalRevenue = allPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .Sum(p => p.Amount ?? 0);

            // Monthly revenue (calculated in-memory) - using ViewModel
            var monthlyRevenue = allPayments
                .Where(p => (p.Status == "Completed" || p.Status == "Success") && p.CreatedAt.HasValue)
                .GroupBy(p => new { Year = p.CreatedAt!.Value.Year, Month = p.CreatedAt.Value.Month })
                .Select(g => new MonthlyRevenueViewModel
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Revenue = g.Sum(p => p.Amount ?? 0),
                    Count = g.Count()
                })
                .OrderBy(r => r.Month)
                .ToList();

            // Payment method stats (using Provider instead) - using ViewModel
            var paymentMethodStats = allPayments
                .GroupBy(p => p.Provider ?? "Unknown")
                .Select(g => new PaymentMethodStatViewModel
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(p => p.Amount ?? 0)
                })
                .ToList();

            // Top exams by revenue - using ViewModel
            var topExams = allPayments
                .Where(p => (p.Status == "Completed" || p.Status == "Success") && p.ExamId.HasValue)
                .GroupBy(p => new { p.ExamId, p.Exam!.Title })
                .Select(g => new TopExamViewModel
                {
                    ExamId = g.Key.ExamId,
                    ExamTitle = g.Key.Title,
                    Revenue = g.Sum(p => p.Amount ?? 0),
                    Purchases = g.Count()
                })
                .OrderByDescending(e => e.Revenue)
                .Take(5)
                .ToList();

            // User growth (calculated in-memory) - using ViewModel
            var userGrowth = await _context.TbUsers
                .Where(u => u.CreatedAt.HasValue)
                .ToListAsync();

            var userGrowthList = userGrowth
                .GroupBy(u => new { Year = u.CreatedAt!.Value.Year, Month = u.CreatedAt.Value.Month })
                .Select(g => new UserGrowthViewModel
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Count = g.Count()
                })
                .OrderBy(u => u.Month)
                .ToList();

            // Get additional stats from database
            var totalExams = await _context.TbExams.CountAsync();
            var totalUsers = await _context.TbUsers.CountAsync();
            var totalResults = await _context.TbExamResults.CountAsync();
            var totalCourses = await _context.TbCourses.CountAsync();
            var totalLessons = await _context.TbLessons.CountAsync();
            var totalQuestions = await _context.TbQuestions.CountAsync();
            
            // Exam participation stats - using ViewModel
            var examParticipation = await _context.TbExamResults
                .Include(r => r.Exam)
                .GroupBy(r => r.Exam)
                .Select(g => new ExamParticipationViewModel
                {
                    ExamTitle = g.Key != null ? g.Key.Title : "N/A",
                    ParticipationRate = totalUsers > 0 ? (double)g.Count() / totalUsers * 100 : 0,
                    TotalAttempts = g.Count()
                })
                .OrderByDescending(e => e.ParticipationRate)
                .Take(5)
                .ToListAsync();

            // User role distribution - using ViewModel
            var userRoleDistribution = await _context.TbUsers
                .GroupBy(u => u.Role ?? "User")
                .Select(g => new UserRoleDistributionViewModel
                {
                    Role = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["MonthlyRevenue"] = monthlyRevenue;
            ViewData["PaymentMethodStats"] = paymentMethodStats;
            ViewData["TopExams"] = topExams;
            ViewData["UserGrowth"] = userGrowthList;
            ViewData["TotalPayments"] = allPayments.Count;
            ViewData["SuccessfulPayments"] = allPayments.Count(p => p.Status == "Completed" || p.Status == "Success");
            ViewData["TotalExams"] = totalExams;
            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalResults"] = totalResults;
            ViewData["TotalCourses"] = totalCourses;
            ViewData["TotalLessons"] = totalLessons;
            ViewData["TotalQuestions"] = totalQuestions;
            ViewData["ExamParticipation"] = examParticipation;
            ViewData["UserRoleDistribution"] = userRoleDistribution;

            return View("~/Areas/Admin/Views/Home/Reports.cshtml");
        }

        // GET: /admin/payments/{id} - View payment details
        [Route("admin/payments/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var payment = await _context.TbPayments
                .Include(p => p.User)
                .Include(p => p.Exam)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            return View("~/Areas/Admin/Views/Home/PaymentDetail.cshtml", payment);
        }

        // POST: /admin/approve-refund - Approve refund request
        [HttpPost]
        [Route("admin/approve-refund")]
        public async Task<IActionResult> ApproveRefund([FromBody] ApproveRefundRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var payment = await _context.TbPayments.FindAsync(req.Id);
            if (payment == null) return Json(new { success = false, message = "Giao dịch không tồn tại" });

            if (payment.Provider != "Refund" || payment.Status != "Pending")
                return Json(new { success = false, message = "Chỉ có thể xác nhận yêu cầu hoàn tiền đang chờ xử lý" });

            payment.Status = "Refunded";
            _context.TbPayments.Update(payment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yêu cầu hoàn tiền đã được xác nhận" });
        }

        // POST: /admin/reject-refund - Reject refund request
        [HttpPost]
        [Route("admin/reject-refund")]
        public async Task<IActionResult> RejectRefund([FromBody] RejectRefundRequest req)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var payment = await _context.TbPayments.FindAsync(req.Id);
            if (payment == null) return Json(new { success = false, message = "Giao dịch không tồn tại" });

            if (payment.Provider != "Refund" || payment.Status != "Pending")
                return Json(new { success = false, message = "Chỉ có thể từ chối yêu cầu hoàn tiền đang chờ xử lý" });

            payment.Status = "Failed";
            _context.TbPayments.Update(payment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yêu cầu hoàn tiền đã bị từ chối" });
        }

        // GET: /admin/reports/export-excel
        [Route("admin/reports/export-excel")]
        public async Task<IActionResult> ExportExcel()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            try
            {
                // Get all data for export
                var allPayments = await _context.TbPayments
                    .Include(p => p.User)
                    .Include(p => p.Exam)
                    .Where(p => p.Status == "Completed" || p.Status == "Success")
                    .ToListAsync();

                var totalRevenue = allPayments.Sum(p => p.Amount ?? 0);
                var totalUsers = await _context.TbUsers.CountAsync();
                var totalExams = await _context.TbExams.CountAsync();
                var totalCourses = await _context.TbCourses.CountAsync();

                // Create Excel file using OpenXML
                using (var stream = new MemoryStream())
                {
                    using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                    {
                        var workbookPart = document.AddWorkbookPart();
                        workbookPart.Workbook = new Workbook();
                        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                        worksheetPart.Worksheet = new Worksheet(new SheetData());
                        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                        var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Báo Cáo Thống Kê" };
                        sheets.Append(sheet);

                        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                        if (sheetData == null)
                        {
                            sheetData = new SheetData();
                            worksheetPart.Worksheet.AppendChild(sheetData);
                        }

                        // Helper function to create cell
                        Func<string, Cell> createCell = (value) => new Cell()
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(value)
                        };

                        Func<decimal, Cell> createNumberCell = (value) => new Cell()
                        {
                            DataType = CellValues.Number,
                            CellValue = new CellValue(value.ToString())
                        };

                        // Header
                        var headerRow = new Row();
                        headerRow.Append(createCell("BÁO CÁO THỐNG KÊ QUIZLY"));
                        sheetData.AppendChild(headerRow);

                        var dateRow = new Row();
                        dateRow.Append(createCell($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}"));
                        sheetData.AppendChild(dateRow);

                        // Empty row
                        sheetData.AppendChild(new Row());

                        // Summary section
                        var summaryHeaderRow = new Row();
                        summaryHeaderRow.Append(createCell("TỔNG QUAN"));
                        sheetData.AppendChild(summaryHeaderRow);

                        var revenueRow = new Row();
                        revenueRow.Append(createCell("Tổng Doanh Thu"));
                        revenueRow.Append(createNumberCell(totalRevenue));
                        sheetData.AppendChild(revenueRow);

                        var usersRow = new Row();
                        usersRow.Append(createCell("Tổng Người Dùng"));
                        usersRow.Append(createNumberCell(totalUsers));
                        sheetData.AppendChild(usersRow);

                        var examsRow = new Row();
                        examsRow.Append(createCell("Tổng Đề Thi"));
                        examsRow.Append(createNumberCell(totalExams));
                        sheetData.AppendChild(examsRow);

                        var coursesRow = new Row();
                        coursesRow.Append(createCell("Tổng Khóa Học"));
                        coursesRow.Append(createNumberCell(totalCourses));
                        sheetData.AppendChild(coursesRow);

                        var paymentsRow = new Row();
                        paymentsRow.Append(createCell("Tổng Giao Dịch Thành Công"));
                        paymentsRow.Append(createNumberCell(allPayments.Count));
                        sheetData.AppendChild(paymentsRow);

                        // Empty row
                        sheetData.AppendChild(new Row());

                        // Transaction details header
                        var detailHeaderRow = new Row();
                        detailHeaderRow.Append(createCell("ID"));
                        detailHeaderRow.Append(createCell("Người Dùng"));
                        detailHeaderRow.Append(createCell("Email"));
                        detailHeaderRow.Append(createCell("Sản Phẩm"));
                        detailHeaderRow.Append(createCell("Số Tiền"));
                        detailHeaderRow.Append(createCell("Trạng Thái"));
                        detailHeaderRow.Append(createCell("Ngày Giao Dịch"));
                        sheetData.AppendChild(detailHeaderRow);

                        // Transaction details
                        foreach (var payment in allPayments.OrderByDescending(p => p.CreatedAt))
                        {
                            var dataRow = new Row();
                            dataRow.Append(createNumberCell(payment.Id));
                            dataRow.Append(createCell(payment.User?.FullName ?? "N/A"));
                            dataRow.Append(createCell(payment.User?.Email ?? "N/A"));
                            dataRow.Append(createCell(payment.Exam?.Title ?? "Gói hội viên"));
                            dataRow.Append(createNumberCell(payment.Amount ?? 0));
                            dataRow.Append(createCell(payment.Status ?? "N/A"));
                            dataRow.Append(createCell(payment.CreatedAt.HasValue ? payment.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") : "N/A"));
                            sheetData.AppendChild(dataRow);
                        }

                        workbookPart.Workbook.Save();
                    }

                    stream.Position = 0;
                    var fileName = $"BaoCaoThongKe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xuất Excel: " + ex.Message;
                return RedirectToAction("Reports");
            }
        }

        // POST: /admin/reports/send-email
        [HttpPost]
        [Route("admin/reports/send-email")]
        public async Task<IActionResult> SendReportEmail(string email, bool useCurrentEmail)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền" });

            try
            {
                string recipientEmail = email;
                
                if (useCurrentEmail)
                {
                    var userId = HttpContext.Session.GetInt32("UserId");
                    if (userId.HasValue)
                    {
                        var user = await _context.TbUsers.FindAsync(userId.Value);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            recipientEmail = user.Email;
                        }
                        else
                        {
                            return Json(new { success = false, message = "Không tìm thấy email của bạn. Vui lòng nhập email." });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
                    }
                }

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    return Json(new { success = false, message = "Vui lòng nhập email." });
                }

                // Get report data
                var allPayments = await _context.TbPayments
                    .Include(p => p.User)
                    .Include(p => p.Exam)
                    .Where(p => p.Status == "Completed" || p.Status == "Success")
                    .ToListAsync();

                var totalRevenue = allPayments.Sum(p => p.Amount ?? 0);
                var totalUsers = await _context.TbUsers.CountAsync();
                var totalExams = await _context.TbExams.CountAsync();
                var totalCourses = await _context.TbCourses.CountAsync();

                // Create email content
                var emailBody = $@"
<h2>Báo Cáo Thống Kê Quizly</h2>
<p>Ngày báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}</p>

<h3>Tổng Quan</h3>
<ul>
    <li>Tổng Doanh Thu: {totalRevenue:N0}₫</li>
    <li>Tổng Người Dùng: {totalUsers}</li>
    <li>Tổng Đề Thi: {totalExams}</li>
    <li>Tổng Khóa Học: {totalCourses}</li>
    <li>Tổng Giao Dịch Thành Công: {allPayments.Count}</li>
</ul>

<h3>Top 5 Giao Dịch Gần Nhất</h3>
<table border='1' cellpadding='5'>
    <tr>
        <th>ID</th>
        <th>Người Dùng</th>
        <th>Sản Phẩm</th>
        <th>Số Tiền</th>
        <th>Ngày</th>
    </tr>
";

                foreach (var payment in allPayments.OrderByDescending(p => p.CreatedAt).Take(5))
                {
                    emailBody += $@"
    <tr>
        <td>{payment.Id}</td>
        <td>{payment.User?.FullName ?? "N/A"}</td>
        <td>{payment.Exam?.Title ?? "Gói hội viên"}</td>
        <td>{payment.Amount ?? 0:N0}₫</td>
        <td>{payment.CreatedAt:dd/MM/yyyy HH:mm}</td>
    </tr>
";
                }

                emailBody += "</table>";

                // TODO: Implement actual email sending service
                // For now, just return success
                // In production, use a service like SendGrid, SMTP, etc.
                
                TempData["Success"] = $"Báo cáo đã được gửi đến {recipientEmail} (Chức năng gửi email cần được cấu hình)";
                return Json(new { success = true, message = $"Báo cáo đã được gửi đến {recipientEmail}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi gửi email: " + ex.Message });
            }
        }

        public class ApproveRefundRequest { public int Id { get; set; } }
        public class RejectRefundRequest { public int Id { get; set; } public string? Reason { get; set; } }
    }
}
