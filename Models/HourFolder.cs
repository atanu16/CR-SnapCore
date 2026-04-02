namespace LumaGallery.Models;

/// <summary>
/// Represents a child hour folder (0–23 format).
/// </summary>
public class HourFolder
{
    public string RawName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Hour { get; set; }
    public int ImageCount { get; set; }
}
