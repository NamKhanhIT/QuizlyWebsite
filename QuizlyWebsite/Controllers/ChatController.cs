using QuizlyWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace Harmic_NNK.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message không được để trống" });
                }

                var response = await _chatService.GetChatResponseAsync(request.Message, cancellationToken);

                return Ok(new ChatResponse
                {
                    Message = response,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi xử lý tin nhắn" });
            }
        }

        [HttpPost("suggest")]

        [HttpPost("image")]
        public async Task<IActionResult> AnalyzeImage([FromForm] string prompt, [FromForm] IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Không có ảnh được gửi lên." });

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var bytes = ms.ToArray();

            string question = string.IsNullOrWhiteSpace(prompt)
                ? "Hãy mô tả nội dung bức ảnh này bằng tiếng Việt."
                : prompt;

            string result = await _chatService.CallGeminiImageAPI(question, bytes);

            return Ok(new { message = result });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
