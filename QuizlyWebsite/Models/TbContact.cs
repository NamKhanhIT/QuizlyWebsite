using System;

namespace QuizlyWebsite.Models;

public partial class TbContact
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public string? Response { get; set; }

    public DateTime? RespondedAt { get; set; }
}

