using System.IO;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;

namespace LumaGallery.Services;

/// <summary>
/// Service for generating and caching image thumbnails.
/// Uses a concurrent dictionary for thread-safe caching.
/// Thumbnails are generated at reduced resolution for performance.
/// </summary>
public class ThumbnailService
{
    private readonly ConcurrentDictionary<string, BitmapImage> _cache = new();
    private const int ThumbnailSize = 400;

    /// <summary>
    /// Gets a cached thumbnail or generates one asynchronously.
    /// </summary>
    public async Task<BitmapImage?> GetThumbnailAsync(string imagePath)
    {
        if (_cache.TryGetValue(imagePath, out var cached))
            return cached;

        return await Task.Run(() => GenerateThumbnail(imagePath));
    }

    /// <summary>
    /// Generates a thumbnail at reduced resolution for fast rendering.
    /// Uses DecodePixelWidth to avoid loading full-resolution images into memory.
    /// </summary>
    private BitmapImage? GenerateThumbnail(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.DecodePixelWidth = ThumbnailSize;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.EndInit();
            bitmap.Freeze(); // Required for cross-thread access

            _cache.TryAdd(imagePath, bitmap);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Loads a full-resolution image for the viewer.
    /// </summary>
    public async Task<BitmapImage?> GetFullImageAsync(string imagePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Clears the thumbnail cache to free memory.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
