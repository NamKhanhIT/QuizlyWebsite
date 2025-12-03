namespace QuizlyWebsite.Models
{
    /// <summary>
    /// Dashboard statistics view model
    /// </summary>
    public class DashboardViewModel
    {
        public int TotalExams { get; set; }
        public int TotalUsers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalResults { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<ExamStatistic> TopExams { get; set; } = new();
    }

    /// <summary>
    /// Exam statistics data
    /// </summary>
    public class ExamStatistic
    {
        public int ExamId { get; set; }
        public string? Title { get; set; }
        public int Attempts { get; set; }
        public decimal? AvgScore { get; set; }
    }

    /// <summary>
    /// Quiz result view model
    /// </summary>
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

    /// <summary>
    /// Question result view model
    /// </summary>
    public class QuestionResultViewModel
    {
        public string? Content { get; set; }
        public string? UserAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}
