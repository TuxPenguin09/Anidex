using Anidex.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Anidex.Controllers;

/// <summary>
/// Streams image blobs attached to comments. Anonymous read is permitted so
/// the discussion list can render thumbnails for unauthenticated viewers —
/// only the POST path (CommentService.AddCommentAsync) is gated by auth.
/// </summary>
[ApiController]
[Route("api/comment/{commentId:guid}/image/{index:int}")]
public class CommentImageController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CommentImageController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Plain DTO so EF can translate the projection (no tuple literals in expression trees).</summary>
    private class ImageSlot
    {
        public byte[]? Bytes { get; set; }
        public string? ContentType { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid commentId, int index)
    {
        if (index < 0 || index > 3)
            return BadRequest("Image index must be between 0 and 3.");

        // Project to the requested slot only so we don't pull the other three
        // blobs (each up to 5 MB) over the wire.
        ImageSlot? slot = index switch
        {
            0 => await _context.Comments
                .Where(c => c.Id == commentId)
                .Select(c => new ImageSlot { Bytes = c.Image1, ContentType = c.Image1ContentType })
                .FirstOrDefaultAsync(),
            1 => await _context.Comments
                .Where(c => c.Id == commentId)
                .Select(c => new ImageSlot { Bytes = c.Image2, ContentType = c.Image2ContentType })
                .FirstOrDefaultAsync(),
            2 => await _context.Comments
                .Where(c => c.Id == commentId)
                .Select(c => new ImageSlot { Bytes = c.Image3, ContentType = c.Image3ContentType })
                .FirstOrDefaultAsync(),
            _ => await _context.Comments
                .Where(c => c.Id == commentId)
                .Select(c => new ImageSlot { Bytes = c.Image4, ContentType = c.Image4ContentType })
                .FirstOrDefaultAsync(),
        };

        if (slot is null || slot.Bytes is null || slot.Bytes.Length == 0)
            return NotFound();

        return File(slot.Bytes, slot.ContentType ?? "image/jpeg");
    }
}
