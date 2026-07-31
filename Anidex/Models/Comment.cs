using System;
using System.ComponentModel.DataAnnotations;

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
    public int AgreeCount { get; set; } = 0;

    // Up to 4 image attachments stored as BLOBs in the database. Both the bytes
    // and the originating content type are kept so the streaming endpoint can
    // serve them back with the correct response headers. Anything past slot 4
    // is dropped — the UI enforces the limit.
    public byte[]? Image1 { get; set; }
    [MaxLength(64)]
    public string? Image1ContentType { get; set; }
    public byte[]? Image2 { get; set; }
    [MaxLength(64)]
    public string? Image2ContentType { get; set; }
    public byte[]? Image3 { get; set; }
    [MaxLength(64)]
    public string? Image3ContentType { get; set; }
    public byte[]? Image4 { get; set; }
    [MaxLength(64)]
    public string? Image4ContentType { get; set; }
}
