using System;
using System.Collections.Generic;

namespace QuizlyWebsite.Models;

public partial class TbBlog
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Thumbnail { get; set; }

    public string? Summary { get; set; }

    public string Content { get; set; } = null!;

    public int AuthorId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsPublished { get; set; }

    public virtual TbUser Author { get; set; } = null!;
}
