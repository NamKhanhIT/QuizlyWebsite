using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbExam
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Title { get; set; } = null!;

    public string? Difficulty { get; set; }

    public int Duration { get; set; }

    public int? QuestionCount { get; set; }

    public decimal? Price { get; set; }

    public bool? IsPremium { get; set; }

    public DateTime? CreatedAt { get; set; }

    public double? AvgRating { get; set; }

    public int? TotalReviews { get; set; }

    public virtual TbSubject Subject { get; set; } = null!;

    public virtual ICollection<TbExamResult> TbExamResults { get; set; } = new List<TbExamResult>();

    public virtual ICollection<TbExamReview> TbExamReviews { get; set; } = new List<TbExamReview>();

    public virtual ICollection<TbExamSession> TbExamSessions { get; set; } = new List<TbExamSession>();

    public virtual ICollection<TbPayment> TbPayments { get; set; } = new List<TbPayment>();

    public virtual ICollection<TbPenaltyRule> TbPenaltyRules { get; set; } = new List<TbPenaltyRule>();

    public virtual ICollection<TbQuestion> TbQuestions { get; set; } = new List<TbQuestion>();

    public virtual ICollection<TbUserPurchase> TbUserPurchases { get; set; } = new List<TbUserPurchase>();
}
