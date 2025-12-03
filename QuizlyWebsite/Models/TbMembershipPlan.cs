using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbMembershipPlan
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public decimal? Price { get; set; }

    public int? DurationDays { get; set; }

    public virtual ICollection<TbUserMembership> TbUserMemberships { get; set; } = new List<TbUserMembership>();
}
