using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbSubject
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public virtual TbCategory Category { get; set; } = null!;

    public virtual ICollection<TbExam> TbExams { get; set; } = new List<TbExam>();
}
