using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuizlyWebsite.Models;
using QuizlyWebsite.Services;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly QuizlyDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            QuizlyDbContext context,
            IEmailSender emailSender,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        // Check admin authorization
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }

        // GET: /admin/reports/export-excel
        [HttpGet]
        [Route("admin/reports/export-excel")]
        public async Task<IActionResult> ExportExcel()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            try
            {
                var excelBytes = await GenerateExcelBytesAsync();
                var fileName = $"BaoCaoThongKe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel report");
                TempData["Error"] = "Lỗi khi xuất báo cáo: " + ex.Message;
                return RedirectToAction("Reports", "Home");
            }
        }

        // POST: /admin/reports/send-email
        [HttpPost]
        [Route("admin/reports/send-email")]
        public async Task<IActionResult> SendEmail(string email, bool useCurrentEmail)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Không có quyền truy cập" });

            try
            {
                // Xác định email nhận báo cáo
                string? finalEmail = null;
                
                if (useCurrentEmail)
                {
                    finalEmail = HttpContext.Session.GetString("Email");
                    if (string.IsNullOrEmpty(finalEmail))
                    {
                        return Json(new { success = false, message = "Không tìm thấy email của admin đang đăng nhập" });
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        return Json(new { success = false, message = "Vui lòng nhập email" });
                    }
                    finalEmail = email.Trim();
                }

                // Sinh file Excel
                var excelBytes = await GenerateExcelBytesAsync();
                var fileName = $"BaoCaoThongKe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Chuẩn bị nội dung email
                var subject = "Báo Cáo Thống Kê - Quizly Website";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2 style='color: #0d141b;'>Báo Cáo Thống Kê</h2>
                        <p>Xin chào,</p>
                        <p>Bạn đã nhận được báo cáo thống kê từ hệ thống Quizly Website.</p>
                        <p>File Excel đính kèm chứa các thông tin:</p>
                        <ul>
                            <li>Doanh thu theo tháng</li>
                            <li>Số giao dịch theo tháng</li>
                            <li>Thống kê chi tiết</li>
                        </ul>
                        <p>Thời gian tạo báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                        <p>Trân trọng,<br/>Hệ thống Quizly Website</p>
                    </body>
                    </html>";

                // Gửi email
                var success = await _emailSender.SendEmailWithAttachmentAsync(
                    finalEmail,
                    subject,
                    body,
                    excelBytes,
                    fileName);

                if (success)
                {
                    return Json(new { success = true, message = $"Báo cáo đã được gửi thành công đến email: {finalEmail}" });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi gửi email. Vui lòng kiểm tra cấu hình SMTP." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email report");
                return Json(new { success = false, message = "Lỗi khi gửi email: " + ex.Message });
            }
        }

        // Hàm dùng chung để sinh file Excel
        private async Task<byte[]> GenerateExcelBytesAsync()
        {
            // Lấy dữ liệu báo cáo từ database
            var monthlyRevenue = await _context.TbPayments
                .Where(p => p.Status == "Completed" || p.Status == "Success")
                .Where(p => p.CreatedAt >= DateTime.Now.AddMonths(-12))
                .GroupBy(p => new { p.CreatedAt.Value.Year, p.CreatedAt.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.Amount ?? 0),
                    Count = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            // Tạo file Excel bằng EPPlus
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Báo Cáo Thống Kê");

            // Đặt tiêu đề
            worksheet.Cells[1, 1].Value = "BÁO CÁO THỐNG KÊ - QUIZLY WEBSITE";
            worksheet.Cells[1, 1, 1, 4].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            worksheet.Cells[2, 1].Value = $"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            worksheet.Cells[2, 1, 2, 4].Merge = true;
            worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Đặt header cho bảng
            int row = 4;
            worksheet.Cells[row, 1].Value = "Tháng";
            worksheet.Cells[row, 2].Value = "Số Giao Dịch";
            worksheet.Cells[row, 3].Value = "Doanh Thu (VND)";
            worksheet.Cells[row, 4].Value = "Ghi Chú";

            // Format header
            using (var range = worksheet.Cells[row, 1, row, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Điền dữ liệu
            row++;
            foreach (var month in monthlyRevenue)
            {
                worksheet.Cells[row, 1].Value = $"{month.Month:00}/{month.Year}";
                worksheet.Cells[row, 2].Value = month.Count;
                worksheet.Cells[row, 3].Value = month.Revenue;
                worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 4].Value = month.Count > 0 ? "Có giao dịch" : "Không có giao dịch";
                row++;
            }

            // Nếu không có dữ liệu
            if (!monthlyRevenue.Any())
            {
                worksheet.Cells[row, 1].Value = "Chưa có dữ liệu";
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                row++;
            }

            // Tổng kết
            row++;
            worksheet.Cells[row, 1].Value = "TỔNG CỘNG";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Value = monthlyRevenue.Sum(m => m.Count);
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            worksheet.Cells[row, 3].Value = monthlyRevenue.Sum(m => m.Revenue);
            worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0";
            worksheet.Cells[row, 3].Style.Font.Bold = true;

            // Tự động căn chỉnh cột
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Thêm border cho toàn bộ bảng
            if (monthlyRevenue.Any())
            {
                using (var range = worksheet.Cells[4, 1, row, 4])
                {
                    range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }
            }

            return package.GetAsByteArray();
        }
    }
}
