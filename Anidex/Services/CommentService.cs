using Anidex.Data;
using Anidex.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anidex.Services;

public class CommentService
{
    private readonly ApplicationDbContext _context;

    public CommentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Comment>> GetCommentsAsync(string mediaId)
    {
        var allComments = await _context.Comments
            .Where(c => c.MediaId == mediaId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return BuildCommentTree(allComments);
    }

    public async Task AddCommentAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Guid id, string userId)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null && comment.UserId == userId)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementAgreeCountAsync(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.AgreeCount++;
            await _context.SaveChangesAsync();
        }
    }

    private List<Comment> BuildCommentTree(List<Comment> allComments)
    {
        // Simple flatten for now, but we can identify replies via ParentCommentId
        return allComments;
    }
}
