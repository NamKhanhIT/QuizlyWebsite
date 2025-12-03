using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbExamResult
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ExamId { get; set; }

    public int? SessionId { get; set; }

    public decimal? Score { get; set; }

    public int? CorrectAnswers { get; set; }

    public int? TotalQuestions { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string? Status { get; set; }

    public virtual TbExam Exam { get; set; } = null!;

    public virtual TbExamSession? Session { get; set; }

    public virtual ICollection<TbExamResultDetail> TbExamResultDetails { get; set; } = new List<TbExamResultDetail>();

    public virtual TbUser User { get; set; } = null!;
}
