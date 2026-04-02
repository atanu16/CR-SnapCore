namespace LumaGallery.Models;

/// <summary>
/// Represents a parent date folder (DDMMYYYY format).
/// </summary>
public class DateFolder
{
    public string RawName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ParsedDate { get; set; }
    public List<HourFolder> HourFolders { get; set; } = new();
    public int TotalImageCount { get; set; }
}
