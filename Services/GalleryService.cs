using System.IO;
using LumaGallery.Helpers;
using LumaGallery.Models;

namespace LumaGallery.Services;

/// <summary>
/// Service responsible for scanning the root directory and building
/// the folder/image hierarchy. Runs asynchronously to avoid UI blocking.
/// </summary>
public class GalleryService
{
    // Supported image extensions
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff", ".tif", ".ico", ".svg"
    };

    /// <summary>
    /// Scans the root directory and returns a list of DateFolder objects
    /// with their child HourFolders populated.
    /// </summary>
    public async Task<List<DateFolder>> ScanRootDirectoryAsync(string rootPath)
    {
        return await Task.Run(() => ScanRootDirectory(rootPath));
    }

    private List<DateFolder> ScanRootDirectory(string rootPath)
    {
        var dateFolders = new List<DateFolder>();

        if (!Directory.Exists(rootPath))
            return dateFolders;

        // Get all subdirectories (parent date folders)
        var directories = Directory.GetDirectories(rootPath);

        foreach (var dir in directories)
        {
            var folderName = Path.GetFileName(dir);
            var parsedDate = DateTimeFormatter.ParseDateFolder(folderName);

            var dateFolder = new DateFolder
            {
                RawName = folderName,
                FullPath = dir,
                DisplayName = DateTimeFormatter.FormatDateFolder(folderName),
                ParsedDate = parsedDate,
                HourFolders = ScanHourFolders(dir)
            };

            // Calculate total image count across all hour folders
            dateFolder.TotalImageCount = dateFolder.HourFolders.Sum(h => h.ImageCount);
            dateFolders.Add(dateFolder);
        }

        // Sort by parsed date descending (newest first)
        dateFolders.Sort((a, b) => b.ParsedDate.CompareTo(a.ParsedDate));

        return dateFolders;
    }

    /// <summary>
    /// Scans hour subdirectories within a date folder.
    /// </summary>
    private List<HourFolder> ScanHourFolders(string dateFolderPath)
    {
        var hourFolders = new List<HourFolder>();
        var directories = Directory.GetDirectories(dateFolderPath);

        foreach (var dir in directories)
        {
            var folderName = Path.GetFileName(dir);
            if (!int.TryParse(folderName, out int hour) || hour < 0 || hour > 23)
                continue;

            var imageCount = CountImages(dir);

            hourFolders.Add(new HourFolder
            {
                RawName = folderName,
                FullPath = dir,
                DisplayName = DateTimeFormatter.FormatHourFolder(folderName),
                Hour = hour,
                ImageCount = imageCount
            });
        }

        // Sort by hour ascending
        hourFolders.Sort((a, b) => a.Hour.CompareTo(b.Hour));

        return hourFolders;
    }

    /// <summary>
    /// Returns images from a specific folder path.
    /// </summary>
    public async Task<List<ImageItem>> GetImagesAsync(string folderPath, string parentDate, string parentHour)
    {
        return await Task.Run(() => GetImages(folderPath, parentDate, parentHour));
    }

    private List<ImageItem> GetImages(string folderPath, string parentDate, string parentHour)
    {
        var images = new List<ImageItem>();

        if (!Directory.Exists(folderPath))
            return images;

        var files = Directory.GetFiles(folderPath);

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (!ImageExtensions.Contains(ext))
                continue;

            var fi = new FileInfo(file);
            images.Add(new ImageItem
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                ParentDate = parentDate,
                ParentHour = parentHour,
                FileSize = fi.Length,
                LastModified = fi.LastWriteTime
            });
        }

        // Sort by filename
        images.Sort((a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));

        return images;
    }

    /// <summary>
    /// Counts image files in a directory.
    /// </summary>
    private int CountImages(string folderPath)
    {
        try
        {
            return Directory.GetFiles(folderPath)
                .Count(f => ImageExtensions.Contains(Path.GetExtension(f)));
        }
        catch
        {
            return 0;
        }
    }
}
