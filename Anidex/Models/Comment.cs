using System;

namespace Anidex.Models;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string MediaId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? MediaUrl { get; set; }
    public int AgreeCount { get; set; } = 0;
}
