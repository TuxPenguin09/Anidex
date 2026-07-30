namespace Anidex.Models;

public class RankedMediaItem
{
    public int Rank { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string Subtitle { get; set; } = string.Empty;
    public string DetailsUrl { get; set; } = "#";
}