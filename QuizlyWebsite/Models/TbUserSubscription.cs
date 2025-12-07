using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbUserSubscription
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? PlanType { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool? IsActive { get; set; }
}
