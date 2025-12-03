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

    public virtual ICollection<TbExamResult> TbExamResults { get; set; } = new List<TbExamResult>();

    public virtual ICollection<TbExamReview> TbExamReviews { get; set; } = new List<TbExamReview>();

    public virtual ICollection<TbExamSession> TbExamSessions { get; set; } = new List<TbExamSession>();

    public virtual ICollection<TbPayment> TbPayments { get; set; } = new List<TbPayment>();

    public virtual ICollection<TbUserMembership> TbUserMemberships { get; set; } = new List<TbUserMembership>();

    public virtual ICollection<TbUserPurchase> TbUserPurchases { get; set; } = new List<TbUserPurchase>();
}
