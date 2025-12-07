using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbLesson
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsPreview { get; set; }

    public virtual TbCourse Course { get; set; } = null!;

    public virtual TbUser? CreatedByNavigation { get; set; }

    public virtual ICollection<TbLessonProgress> TbLessonProgresses { get; set; } = new List<TbLessonProgress>();
}
