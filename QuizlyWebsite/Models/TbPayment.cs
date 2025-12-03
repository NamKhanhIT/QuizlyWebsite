using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbPayment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ExamId { get; set; }

    public decimal? Amount { get; set; }

    public string? Currency { get; set; }

    public string? Provider { get; set; }

    public string? ProviderTransId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual TbExam? Exam { get; set; }

    public virtual TbUser User { get; set; } = null!;
}
