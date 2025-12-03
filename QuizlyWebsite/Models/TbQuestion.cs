using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbQuestion
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public string Content { get; set; } = null!;

    public string OptionA { get; set; } = null!;

    public string OptionB { get; set; } = null!;

    public string OptionC { get; set; } = null!;

    public string OptionD { get; set; } = null!;

    public string? CorrectOption { get; set; }

    public decimal? Marks { get; set; }

    public virtual TbExam Exam { get; set; } = null!;

    public virtual ICollection<TbExamResultDetail> TbExamResultDetails { get; set; } = new List<TbExamResultDetail>();
}
