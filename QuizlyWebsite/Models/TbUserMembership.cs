using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbUserMembership
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PlanId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public virtual TbMembershipPlan Plan { get; set; } = null!;

    public virtual TbUser User { get; set; } = null!;
}
