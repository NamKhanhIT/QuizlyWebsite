using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbExamReview
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ExamId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual TbExam Exam { get; set; } = null!;

    public virtual TbUser User { get; set; } = null!;
}
