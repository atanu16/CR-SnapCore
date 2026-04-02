using System.Windows.Media.Imaging;
using LumaGallery.Models;
using LumaGallery.Services;

namespace LumaGallery.ViewModels;

/// <summary>
/// ViewModel wrapper for individual image items.
/// Handles lazy thumbnail loading and favorite toggling.
/// </summary>
public class ImageItemViewModel : ViewModelBase
{
    private readonly ThumbnailService _thumbnailService;
    private BitmapImage? _thumbnail;
    private bool _isLoading = true;
    private bool _isFavorite;

    public ImageItem Model { get; }

    public string FilePath => Model.FilePath;
    public string FileName => Model.FileName;
    public long FileSize => Model.FileSize;
    public DateTime LastModified => Model.LastModified;

    public BitmapImage? Thumbnail
    {
        get => _thumbnail;
        set => SetProperty(ref _thumbnail, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
                Model.IsFavorite = value;
        }
    }

    public ImageItemViewModel(ImageItem model, ThumbnailService thumbnailService)
    {
        Model = model;
        _thumbnailService = thumbnailService;
        _isFavorite = model.IsFavorite;
    }

    /// <summary>
    /// Loads the thumbnail asynchronously. Called when the item enters the viewport.
    /// </summary>
    public async Task LoadThumbnailAsync()
    {
        if (Thumbnail != null) return;

        IsLoading = true;
        Thumbnail = await _thumbnailService.GetThumbnailAsync(FilePath);
        IsLoading = false;
    }
}
