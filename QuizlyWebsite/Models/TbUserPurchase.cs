using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbUserPurchase
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ExamId { get; set; }

    public DateTime? PurchasedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public virtual TbExam Exam { get; set; } = null!;

    public virtual TbUser User { get; set; } = null!;
}
