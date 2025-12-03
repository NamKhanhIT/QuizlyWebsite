using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbMenu
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Url { get; set; } = null!;

    public int? ParentId { get; set; }

    public int? Order { get; set; }

    public string? Location { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<TbMenu> InverseParent { get; set; } = new List<TbMenu>();

    public virtual TbMenu? Parent { get; set; }
}
