//Đoạn này dùng để khai báo các view model cho admin.
namespace QuizlyWebsite.Models
{

    public class DashboardViewModel
    {
        public int TotalExams { get; set; }
        public int TotalUsers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalResults { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<ExamStatistic> TopExams { get; set; } = new();
    }

    public class ExamStatistic
    {
        public int ExamId { get; set; }
        public string? Title { get; set; }
        public int Attempts { get; set; }
        public decimal? AvgScore { get; set; }
    }

    public class QuizResultViewModel
    {
        public int Id { get; set; }
        public string? ExamTitle { get; set; }
        public decimal? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<QuestionResultViewModel> Questions { get; set; } = new();
    }

    public class QuestionResultViewModel
    {
        public string? Content { get; set; }
        public string? UserAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class RankingViewModel
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public int Score { get; set; }
        public string ScoreLabel { get; set; } = "";
        public string? AdditionalInfo { get; set; }
    }

    public class MonthlyRevenueViewModel
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Count { get; set; }
    }

    public class TopExamViewModel
    {
        public int? ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Purchases { get; set; }
    }

    public class UserGrowthViewModel
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PaymentMethodStatViewModel
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ExamParticipationViewModel
    {
        public string ExamTitle { get; set; } = string.Empty;
        public double ParticipationRate { get; set; }
        public int TotalAttempts { get; set; }
    }

    public class UserRoleDistributionViewModel
    {
        public string Role { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
//Đoạn này dùng để đọc đề thi từ file Word hoặc từ văn bản theo định dạng chuẩn.
namespace QuizlyWebsite.Models.ViewModels
{
    public class QuestionParseResult
    {
        public List<ParsedQuestion> ValidQuestions { get; set; } = new();

        public ParseReport Report { get; set; } = new();
    }

    public class ParsedQuestion
    {
        public string Question { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty;
    }

    public class ParseReport
    {
        public int TotalQuestions { get; set; }

        public int ValidCount { get; set; }

        public int ErrorCount { get; set; }

        public List<QuestionError> Errors { get; set; } = new();
    }

    public class QuestionError
    {
        public int QuestionNumber { get; set; }

        public string? QuestionContent { get; set; }

        public string ErrorReason { get; set; } = string.Empty;
    }
}