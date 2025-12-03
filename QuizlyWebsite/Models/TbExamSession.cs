using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbExamSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ExamId { get; set; }

    public Guid? SessionToken { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? LastHeartbeat { get; set; }

    public string? Status { get; set; }

    public bool? AppliedPenalty { get; set; }

    public virtual TbExam Exam { get; set; } = null!;

    public virtual ICollection<TbExamResult> TbExamResults { get; set; } = new List<TbExamResult>();

    public virtual ICollection<TbExamViolation> TbExamViolations { get; set; } = new List<TbExamViolation>();

    public virtual TbUser User { get; set; } = null!;
}
