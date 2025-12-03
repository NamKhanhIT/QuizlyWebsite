using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbPenaltyRule
{
    public int Id { get; set; }

    public int? ExamId { get; set; }

    public string? Reason { get; set; }

    public int? PenaltyPercent { get; set; }

    public virtual TbExam? Exam { get; set; }
}
