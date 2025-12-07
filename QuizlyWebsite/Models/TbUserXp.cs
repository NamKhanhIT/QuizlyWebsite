using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbUserXp
{
    public int UserId { get; set; }

    public int? Xp { get; set; }

    public int? Level { get; set; }

    public virtual TbUser User { get; set; } = null!;
}
