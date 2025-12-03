using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbExamViolation
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public string? ViolationType { get; set; }

    public DateTime? ViolationTime { get; set; }

    public virtual TbExamSession Session { get; set; } = null!;
}
