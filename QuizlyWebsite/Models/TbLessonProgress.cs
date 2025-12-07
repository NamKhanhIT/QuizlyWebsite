using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbLessonProgress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int LessonId { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual TbLesson Lesson { get; set; } = null!;

    public virtual TbUser User { get; set; } = null!;
}
