namespace Weblog.Core.Model.DTOs;

public class LinkPreviewDto
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string Domain { get; set; } = string.Empty;
}

public class LinkPreviewRequest
{
    public string Url { get; set; } = string.Empty;
}