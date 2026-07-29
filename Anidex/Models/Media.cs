namespace Anidex.Models;

public class Media
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // MAL or VNDB
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public double? Score { get; set; }
    public string? Released { get; set; }
    public bool IsAdult { get; set; } = false;
}
