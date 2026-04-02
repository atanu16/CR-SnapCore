namespace LumaGallery.Models;

/// <summary>
/// Represents a single image file within the gallery.
/// </summary>
public class ImageItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ParentDate { get; set; } = string.Empty;
    public string ParentHour { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsFavorite { get; set; }
}
