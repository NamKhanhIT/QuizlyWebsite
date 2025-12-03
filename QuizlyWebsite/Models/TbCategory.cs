using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbCategory
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<TbSubject> TbSubjects { get; set; } = new List<TbSubject>();
}
