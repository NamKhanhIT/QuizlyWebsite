using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbExamResultDetail
{
    public int Id { get; set; }

    public int ExamResultId { get; set; }

    public int QuestionId { get; set; }

    public string? SelectedOption { get; set; }

    public bool? IsCorrect { get; set; }

    public virtual TbExamResult ExamResult { get; set; } = null!;

    public virtual TbQuestion Question { get; set; } = null!;
}
