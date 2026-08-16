namespace AutomotiveInfo.Models;

public class NewsArticleDto
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Tag { get; set; }
    public string? ImageUrl { get; set; }
}