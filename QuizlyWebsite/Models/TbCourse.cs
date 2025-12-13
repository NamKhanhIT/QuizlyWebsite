using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbCourse
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? CreatedBy { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsPaid { get; set; }

    public int? FreeLessonCount { get; set; }

    public string? ImageUrl { get; set; }

    public virtual TbUser? CreatedByNavigation { get; set; }

    public virtual ICollection<TbLesson> TbLessons { get; set; } = new List<TbLesson>();
}
