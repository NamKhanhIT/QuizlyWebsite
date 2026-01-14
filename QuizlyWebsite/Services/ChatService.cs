using QuizlyWebsite.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace QuizlyWebsite.Services
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string userMessage, CancellationToken cancellationToken = default);
        Task<string> GetProductSuggestionsAsync(string userMessage, CancellationToken cancellationToken = default);
        Task<string> CallGeminiImageAPI(string prompt, byte[] imageBytes);
        Task<string> CallGeminiAPI(string prompt, string context = "general");
    }

    public class ChatService : IChatService
    {
        private readonly QuizlyDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public ChatService(QuizlyDbContext context, IConfiguration configuration, ILogger<ChatService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            _apiKey = _configuration["GeminiAI:ApiKey"] ?? string.Empty;
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Gemini API Key is not configured. Please set it in appsettings.json");
            }

            _httpClient = new HttpClient();
        }
        public async Task<string> GetChatResponseAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var systemPrompt = await GetEnhancedSystemPromptAsync();
                var fullPrompt = $"{systemPrompt}\n\nNgười dùng: {userMessage}\n\nTrợ lý:";

                var response = await CallGeminiAPI(fullPrompt);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response");
                return "Xin lỗi, tôi gặp một số vấn đề kỹ thuật. Vui lòng thử lại sau.";
            }
        }

        public async Task<string> GetProductSuggestionsAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var exams = await GetExamsSummaryAsync();
                var courses = await GetCoursesSummaryAsync();
                var plans = await GetMembershipPlansSummaryAsync();
                var contactInfo = GetContactPageInfo();

                var suggestionsPrompt = $@"Bạn là trợ lý AI thông minh cho website Quizly - nền tảng học tập trực tuyến với các đề thi, khóa học và gói trả phí.
                    DANH SÁCH ĐỀ THI:
                    {exams}

                    DANH SÁCH KHÓA HỌC:
                    {courses}

                    GÓI TRẢ PHÍ:
                    {plans}

                    LIÊN HỆ & TÍNH NĂNG:
                    {contactInfo}

                    Dựa trên yêu cầu của người dùng, hãy gợi ý các đề thi, khóa học, hoặc gói trả phí phù hợp. 
                    Nếu người dùng hỏi về liên hệ, cung cấp thông tin liên hệ và đường link tới trang liên hệ.
                    QUAN TRỌNG: Hãy trả lời bằng tiếng Việt, thân thiện và chuyên nghiệp. KHÔNG SỬ DỤNG ** (dấu hoa thị) cho định dạng. Dùng dấu gạch ngang (-) hoặc dấu cộng (+) cho danh sách.

                    Yêu cầu của người dùng: {userMessage}";
                var response = await CallGeminiAPI(suggestionsPrompt);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product suggestions");
                return "Xin lỗi, tôi không thể tìm được gợi ý phù hợp. Vui lòng thử lại sau.";
            }
        }

        private async Task<string> GetEnhancedSystemPromptAsync()
        {
            return $@"Bạn là trợ lý AI thông minh cho website Quizly - nền tảng học tập trực tuyến toàn diện.
            CHỨC NĂNG CHÍNH CỦA WEBSITE:
            1. Đề Thi - Cung cấp hàng ngàn đề thi từ các chủ đề khác nhau
               - Một số đề thi miễn phí cho tất cả người dùng
               - Một số đề thi chỉ dành cho thành viên Premium
            2. Khóa Học - Các khóa học chi tiết từ cơ bản đến nâng cao
               - Khóa học miễn phí với số lượng bài học hạn chế
               - Khóa học trả phí với toàn bộ nội dung
            3. Gói Membership - Nâng cấp tài khoản để có quyền truy cập không giới hạn
            4. Chat Support - Hỗ trợ tư vấn trực tiếp (đây là tôi!)
            5. Theo Dõi Tiến Độ - Xem lịch sử học tập và kết quả thi
            6. Bảng Xếp Hạng - Cạnh tranh với người dùng khác

            Nhiệm vụ của tôi:
            - Trả lời các câu hỏi về đề thi, khóa học, và gói trả phí
            - Gợi ý các sản phẩm phù hợp với nhu cầu của người dùng
            - Hướng dẫn cách sử dụng website
            - Cung cấp thông tin liên hệ nếu cần hỗ trợ thêm
            - Luôn thân thiện, chuyên nghiệp và hữu ích
            
            QUAN TRỌNG: Trong trả lời của bạn, KHÔNG SỬ DỤNG ** (dấu hoa thị đôi) để định dạng. Thay vào đó, dùng dấu gạch ngang (-) hoặc dấu cộng (+) cho danh sách, và ngoặc vuông [text] nếu cần nhấn mạnh.";
        }

        private async Task<string> GetExamsSummaryAsync()
        {
            try
            {
                var exams = await _context.TbExams
                    .Where(e => e.IsApproved == true)
                    .Include(e => e.Subject)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                if (!exams.Any())
                    return "Hiện tại chưa có đề thi nào.";

                var sb = new StringBuilder();
                var freeExams = exams.Where(e => e.IsPaid != true).ToList();
                var paidExams = exams.Where(e => e.IsPaid == true).ToList();

                if (freeExams.Any())
                {
                    sb.AppendLine("🆓 ĐỀ THI MIỄN PHÍ:");
                    foreach (var exam in freeExams.Take(10))
                    {
                        sb.AppendLine($"  + {exam.Title} ({exam.Subject?.Title ?? "Không xác định"})");
                        sb.AppendLine($"    - Độ khó: {exam.Difficulty ?? "N/A"}");
                        sb.AppendLine($"    - Thời gian: {exam.Duration} phút");
                        sb.AppendLine($"    - Đánh giá: {exam.AvgRating?.ToString("F1") ?? "Chưa có"}/5 ({exam.TotalReviews ?? 0} đánh giá)");
                    }
                }

                if (paidExams.Any())
                {
                    sb.AppendLine("\n💰 ĐỀ THI TRẢ PHÍ:");
                    foreach (var exam in paidExams.Take(10))
                    {
                        sb.AppendLine($"  + {exam.Title} ({exam.Subject?.Title ?? "Không xác định"})");
                        sb.AppendLine($"    - Giá: {exam.Price?.ToString("C") ?? "Liên hệ"}");
                        sb.AppendLine($"    - Độ khó: {exam.Difficulty ?? "N/A"}");
                        sb.AppendLine($"    - Thời gian: {exam.Duration} phút");
                        sb.AppendLine($"    - Đánh giá: {exam.AvgRating?.ToString("F1") ?? "Chưa có"}/5");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exams summary");
                return "Không thể tải danh sách đề thi.";
            }
        }

        private async Task<string> GetCoursesSummaryAsync()
        {
            try
            {
                var courses = await _context.TbCourses
                    .Where(c => c.IsApproved == true)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(15)
                    .ToListAsync();

                if (!courses.Any())
                    return "Hiện tại chưa có khóa học nào.";

                var sb = new StringBuilder();
                var freeCourses = courses.Where(c => c.IsPaid != true).ToList();
                var paidCourses = courses.Where(c => c.IsPaid == true).ToList();

                if (freeCourses.Any())
                {
                    sb.AppendLine("🆓 KHÓA HỌC MIỄN PHÍ:");
                    foreach (var course in freeCourses.Take(8))
                    {
                        var lessonCount = course.TbLessons?.Count ?? 0;
                        sb.AppendLine($"  + {course.Title}");
                        sb.AppendLine($"    - {lessonCount} bài học");
                        if (course.FreeLessonCount.HasValue)
                            sb.AppendLine($"    - Bài học miễn phí: {course.FreeLessonCount}");
                    }
                }

                if (paidCourses.Any())
                {
                    sb.AppendLine("\nKHÓA HỌC TRẢ PHÍ:");
                    foreach (var course in paidCourses.Take(8))
                    {
                        var lessonCount = course.TbLessons?.Count ?? 0;
                        sb.AppendLine($"  + {course.Title}");
                        sb.AppendLine($"    - {lessonCount} bài học (cần membership)");
                        if (!string.IsNullOrEmpty(course.Description))
                            sb.AppendLine($"    - Mô tả: {course.Description.Substring(0, Math.Min(60, course.Description.Length))}...");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses summary");
                return "Không thể tải danh sách khóa học.";
            }
        }

        private async Task<string> GetMembershipPlansSummaryAsync()
        {
            try
            {
                var plans = await _context.TbMembershipPlans
                    .OrderBy(p => p.Price)
                    .ToListAsync();

                if (!plans.Any())
                    return "Hiện tại không có gói membership nào.";

                var sb = new StringBuilder();
                sb.AppendLine("💳 GÓI MEMBERSHIP AVAILABLE:");

                foreach (var plan in plans)
                {
                    sb.AppendLine($"  + {plan.Title ?? "Gói không tên"}");
                    sb.AppendLine($"    - Giá: {plan.Price?.ToString("C") ?? "Liên hệ"} / {plan.DurationDays} ngày");
                    sb.AppendLine($"    - Quyền lợi: Truy cập không giới hạn tất cả đề thi & khóa học trả phí");
                }

                sb.AppendLine("\n LỢI ÍCH CỦA MEMBERSHIP:");
                sb.AppendLine("  ✓ Truy cập 100% nội dung khóa học trả phí");
                sb.AppendLine("  ✓ Làm các đề thi Premium không giới hạn");
                sb.AppendLine("  ✓ Xem chi tiết giải thích cho mỗi câu hỏi");
                sb.AppendLine("  ✓ Theo dõi lịch sử học tập chi tiết");
                sb.AppendLine("  ✓ Hỗ trợ ưu tiên từ đội ngũ");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting membership plans summary");
                return "Không thể tải danh sách gói membership.";
            }
        }

        private string GetContactPageInfo()
        {
            return @"THÔNG TIN LIÊN HỆ:
- Website: Quizly - Nền tảng học tập trực tuyến
- Liên hệ: /contact hoặc dùng biểu mẫu trên website
- Email: Gửi qua trang liên hệ (contact page)

TÍNH NĂNG NỔI BẬT CỦA WEBSITE:
- Hàng ngàn đề thi đa dạng từ các chủ đề khác nhau
- Khóa học từ cơ bản đến nâng cao với hướng dẫn chi tiết
- Hệ thống chấm điểm tự động và phân tích kết quả
- Bảng xếp hạng cạnh tranh để khuyến khích học tập
- Theo dõi tiến độ học tập chi tiết
- Hỗ trợ 24/7 qua trợ lý AI
- Giao diện thân thiện, dễ sử dụng
- Tương thích với mọi thiết bị (máy tính, điện thoại)
- Cộng đồng học tập tích cực
- Cập nhật nội dung thường xuyên

DÀNH CHO AI:
- Học sinh chuẩn bị kỳ thi
- Sinh viên muốn nâng cao kỹ năng
- Người muốn học kỹ năng mới
- Bất cứ ai yêu thích học tập";
        }
        public async Task<string> CallGeminiImageAPI(string prompt, byte[] imageBytes)
        {
            var base64 = Convert.ToBase64String(imageBytes);
            var models = new[] { "gemini-2.5-flash", "gemini-2.5-pro", "gemini-pro-vision" };

            foreach (var modelName in models)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";
                    _logger.LogInformation("Trying model: {Model}", modelName);

                    var requestBody = new
                    {
                        contents = new[]
                        {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/png",
                                    data = base64
                                }
                            }
                        }
                    }
                }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        var response = await _httpClient.PostAsync(url, content, cts.Token);
                        var responseText = await response.Content.ReadAsStringAsync();

                        _logger.LogInformation("Model {Model} - Status: {Status}", modelName, response.StatusCode);

                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                var result = JsonSerializer.Deserialize<GeminiResponse>(responseText);
                                var text = result?.candidates?[0]?.content?.parts?[0]?.text;
                                if (!string.IsNullOrEmpty(text))
                                    return text;
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse JSON response from model {Model}", modelName);
                            }
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Timeout with model {Model}", modelName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error with model {Model}", modelName);
                }
            }

            return "Không thể phân tích ảnh. Vui lòng kiểm tra API Key và thử lại.";
        }

        public async Task<string> CallGeminiAPI(string prompt, string context = "general")
        {
            // Try different models in order
            var models = new[] { "gemini-2.5-flash", "gemini-2.5-pro", "gemini-pro" };

            foreach (var modelName in models)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";

                    _logger.LogInformation("Trying model: {Model}", modelName);

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Add timeout
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        var response = await _httpClient.PostAsync(url, content, cts.Token);
                        var responseText = await response.Content.ReadAsStringAsync();

                        _logger.LogInformation("Model {Model} - Status: {Status}", modelName, response.StatusCode);

                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                var result = JsonSerializer.Deserialize<GeminiResponse>(responseText);

                                if (result?.candidates != null && result.candidates.Length > 0)
                                {
                                    var text = result.candidates[0].content?.parts?[0]?.text;
                                    if (!string.IsNullOrEmpty(text))
                                    {
                                        _logger.LogInformation("Success with model: {Model}", modelName);
                                        return text;
                                    }
                                }
                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogWarning(jsonEx, "Failed to parse JSON response from model {Model}", modelName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Model {Model} failed: {Status} - {Response}", modelName, response.StatusCode, 
                                responseText.Length > 200 ? responseText.Substring(0, 200) : responseText);
                            continue;
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Timeout with model {Model}", modelName);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error with model {Model}", modelName);
                    continue;
                }
            }

            return "Xin lỗi, tôi không thể kết nối với AI service lúc này. Vui lòng:\n1. Kiểm tra API Key trong appsettings.json\n2. Đảm bảo kết nối Internet hoạt động\n3. Thử lại sau vài giây";
        }

        private string GetSystemPrompt(string products, string categories, string blogs)
        {
            return $@"Bạn là chuyên viên tư vấn thông minh của Quizly. 
                    Nhiệm vụ của bạn là:
                    1. Trả lời các câu hỏi về đề thi, khóa học, gói membership
                    2. Gợi ý sản phẩm phù hợp cho người dùng
                    3. Hỗ trợ người dùng tìm kiếm và sử dụng website

                    Lưu ý:
                    - Luôn trả lời bằng tiếng Việt
                    - Thân thiện, chuyên nghiệp và nhiệt tình
                    - Đề xuất các sản phẩm phù hợp với nhu cầu người dùng
                    - Cung cấp thông tin liên hệ nếu cần";
        }
    }

    public class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public ContentRequest[] contents { get; set; } = Array.Empty<ContentRequest>();
    }

    public class ContentRequest
    {
        [JsonPropertyName("parts")]
        public PartRequest[] parts { get; set; } = Array.Empty<PartRequest>();
    }

    public class PartRequest
    {
        [JsonPropertyName("text")]
        public string text { get; set; } = string.Empty;
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[]? candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public ContentResponse? content { get; set; }
    }

    public class ContentResponse
    {
        [JsonPropertyName("parts")]
        public PartResponse[]? parts { get; set; }
    }

    public class PartResponse
    {
        [JsonPropertyName("text")]
        public string? text { get; set; }
    }
}