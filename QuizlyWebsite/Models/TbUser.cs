using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Role { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<TbBlog> TbBlogs { get; set; } = new List<TbBlog>();

    public virtual ICollection<TbCourse> TbCourses { get; set; } = new List<TbCourse>();

    public virtual ICollection<TbExam> TbExamApprovedByNavigations { get; set; } = new List<TbExam>();

    public virtual ICollection<TbExam> TbExamCreatedByNavigations { get; set; } = new List<TbExam>();

    public virtual ICollection<TbExamResult> TbExamResults { get; set; } = new List<TbExamResult>();

    public virtual ICollection<TbExamReview> TbExamReviews { get; set; } = new List<TbExamReview>();

    public virtual ICollection<TbExamSession> TbExamSessions { get; set; } = new List<TbExamSession>();

    public virtual ICollection<TbLessonProgress> TbLessonProgresses { get; set; } = new List<TbLessonProgress>();

    public virtual ICollection<TbLesson> TbLessons { get; set; } = new List<TbLesson>();

    public virtual ICollection<TbPayment> TbPayments { get; set; } = new List<TbPayment>();

    public virtual ICollection<TbUserMembership> TbUserMemberships { get; set; } = new List<TbUserMembership>();

    public virtual ICollection<TbUserPurchase> TbUserPurchases { get; set; } = new List<TbUserPurchase>();

    public virtual TbUserXp? TbUserXp { get; set; }
}
