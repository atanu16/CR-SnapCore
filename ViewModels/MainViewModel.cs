using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using LumaGallery.Helpers;
using LumaGallery.Models;
using LumaGallery.Services;

namespace LumaGallery.ViewModels;

/// <summary>
/// Main ViewModel for the gallery application.
/// Manages folder navigation, image loading, and the image viewer state.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly GalleryService _galleryService;
    private readonly ThumbnailService _thumbnailService;

    // Root path to scan
    private string _rootPath = @"\\moh-nt-file1\MOH\Shared Projects\CI_Private\CR\VM_WORKS";

    #region Collections

    public ObservableCollection<DateFolder> DateFolders { get; } = new();
    public ObservableCollection<HourFolder> HourFolders { get; } = new();
    public ObservableCollection<ImageItemViewModel> Images { get; } = new();

    #endregion

    #region Selected Items

    private DateFolder? _selectedDateFolder;
    public DateFolder? SelectedDateFolder
    {
        get => _selectedDateFolder;
        set
        {
            if (SetProperty(ref _selectedDateFolder, value))
            {
                OnPropertyChanged(nameof(SelectedDateDisplay));
                OnPropertyChanged(nameof(HasSelectedDate));
                LoadHourFolders();
            }
        }
    }

    private HourFolder? _selectedHourFolder;
    public HourFolder? SelectedHourFolder
    {
        get => _selectedHourFolder;
        set
        {
            if (SetProperty(ref _selectedHourFolder, value))
            {
                OnPropertyChanged(nameof(SelectedTimeDisplay));
                OnPropertyChanged(nameof(HasSelectedHour));
                OnPropertyChanged(nameof(BreadcrumbText));
                _ = LoadImagesAsync();
            }
        }
    }

    private ImageItemViewModel? _selectedImage;
    public ImageItemViewModel? SelectedImage
    {
        get => _selectedImage;
        set => SetProperty(ref _selectedImage, value);
    }

    #endregion

    #region Viewer State

    private bool _isViewerOpen;
    public bool IsViewerOpen
    {
        get => _isViewerOpen;
        set => SetProperty(ref _isViewerOpen, value);
    }

    private BitmapImage? _viewerImage;
    public BitmapImage? ViewerImage
    {
        get => _viewerImage;
        set => SetProperty(ref _viewerImage, value);
    }

    private string _viewerFileName = string.Empty;
    public string ViewerFileName
    {
        get => _viewerFileName;
        set => SetProperty(ref _viewerFileName, value);
    }

    private string _viewerInfo = string.Empty;
    public string ViewerInfo
    {
        get => _viewerInfo;
        set => SetProperty(ref _viewerInfo, value);
    }

    private int _viewerCurrentIndex;
    public int ViewerCurrentIndex
    {
        get => _viewerCurrentIndex;
        set
        {
            if (SetProperty(ref _viewerCurrentIndex, value))
                OnPropertyChanged(nameof(ViewerPositionText));
        }
    }

    public string ViewerPositionText => Images.Count > 0
        ? $"{ViewerCurrentIndex + 1} of {Images.Count}"
        : string.Empty;

    private double _zoomLevel = 1.0;
    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0.1, 5.0));
    }

    private bool _showInfoOverlay;
    public bool ShowInfoOverlay
    {
        get => _showInfoOverlay;
        set => SetProperty(ref _showInfoOverlay, value);
    }

    #endregion

    #region UI State

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterDateFolders();
        }
    }

    private string _searchImageText = string.Empty;
    public string SearchImageText
    {
        get => _searchImageText;
        set
        {
            if (SetProperty(ref _searchImageText, value))
                FilterImages();
        }
    }

    public string SelectedDateDisplay => SelectedDateFolder?.DisplayName ?? "Select a date";
    public string SelectedTimeDisplay => SelectedHourFolder?.DisplayName ?? "";
    public bool HasSelectedDate => SelectedDateFolder != null;
    public bool HasSelectedHour => SelectedHourFolder != null;

    public string BreadcrumbText
    {
        get
        {
            if (SelectedDateFolder == null) return "Gallery";
            if (SelectedHourFolder == null) return SelectedDateFolder.DisplayName;
            return $"{SelectedDateFolder.DisplayName}  ›  {SelectedHourFolder.DisplayName}";
        }
    }

    #endregion

    #region Commands

    public RelayCommand RefreshCommand { get; }
    public RelayCommand OpenImageCommand { get; }
    public RelayCommand CloseViewerCommand { get; }
    public RelayCommand NextImageCommand { get; }
    public RelayCommand PrevImageCommand { get; }
    public RelayCommand ToggleFavoriteCommand { get; }
    public RelayCommand ZoomInCommand { get; }
    public RelayCommand ZoomOutCommand { get; }
    public RelayCommand ResetZoomCommand { get; }
    public RelayCommand ToggleInfoCommand { get; }
    public RelayCommand SelectAllHoursCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }

    private bool _isDarkMode = true;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    #endregion

    // Backup of all date folders for search filtering
    private List<DateFolder> _allDateFolders = new();

    // Backup of all images for search filtering
    private List<ImageItemViewModel> _allImages = new();

    public MainViewModel()
    {
        _galleryService = new GalleryService();
        _thumbnailService = new ThumbnailService();

        // Initialize commands
        RefreshCommand = new RelayCommand(async () => await ScanDirectoryAsync());
        OpenImageCommand = new RelayCommand(p => OpenImage(p as ImageItemViewModel));
        CloseViewerCommand = new RelayCommand(() => CloseViewer());
        NextImageCommand = new RelayCommand(() => NavigateImage(1));
        PrevImageCommand = new RelayCommand(() => NavigateImage(-1));
        ToggleFavoriteCommand = new RelayCommand(() => ToggleFavorite());
        ZoomInCommand = new RelayCommand(() => ZoomLevel *= 1.25);
        ZoomOutCommand = new RelayCommand(() => ZoomLevel /= 1.25);
        ResetZoomCommand = new RelayCommand(() => ZoomLevel = 1.0);
        ToggleInfoCommand = new RelayCommand(() => ShowInfoOverlay = !ShowInfoOverlay);
        SelectAllHoursCommand = new RelayCommand(() => LoadAllImagesForDate());
        ToggleThemeCommand = new RelayCommand(() =>
        {
            IsDarkMode = !IsDarkMode;
            ThemeService.Apply(IsDarkMode);
        });

        // Auto-scan on startup
        _ = ScanDirectoryAsync();
    }

    /// <summary>
    /// Scans the root directory and populates the date folder list.
    /// </summary>
    public async Task ScanDirectoryAsync()
    {
        IsLoading = true;
        StatusText = "Scanning folders...";

        try
        {
            var folders = await _galleryService.ScanRootDirectoryAsync(_rootPath);
            _allDateFolders = folders;

            DateFolders.Clear();
            foreach (var folder in folders)
                DateFolders.Add(folder);

            StatusText = $"{DateFolders.Count} date folders found";

            // Auto-select first folder if available
            if (DateFolders.Count > 0)
                SelectedDateFolder = DateFolders[0];
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads hour subfolders for the selected date folder.
    /// </summary>
    private void LoadHourFolders()
    {
        HourFolders.Clear();
        Images.Clear();
        SelectedHourFolder = null;

        if (SelectedDateFolder == null) return;

        foreach (var hour in SelectedDateFolder.HourFolders)
            HourFolders.Add(hour);

        // Auto-select first hour if available
        if (HourFolders.Count > 0)
            SelectedHourFolder = HourFolders[0];

        OnPropertyChanged(nameof(BreadcrumbText));
    }

    /// <summary>
    /// Loads images from the selected hour folder asynchronously.
    /// </summary>
    private async Task LoadImagesAsync()
    {
        Images.Clear();
        _allImages.Clear();

        if (SelectedDateFolder == null || SelectedHourFolder == null) return;

        IsLoading = true;
        StatusText = "Loading images...";

        try
        {
            var images = await _galleryService.GetImagesAsync(
                SelectedHourFolder.FullPath,
                SelectedDateFolder.DisplayName,
                SelectedHourFolder.DisplayName
            );

            foreach (var img in images)
            {
                var vm = new ImageItemViewModel(img, _thumbnailService);
                _allImages.Add(vm);
                // Fire-and-forget thumbnail loading for each image
                _ = vm.LoadThumbnailAsync();
            }

            FilterImages();
            StatusText = $"{_allImages.Count} images";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads all images across all hour folders for the selected date.
    /// </summary>
    private async void LoadAllImagesForDate()
    {
        if (SelectedDateFolder == null) return;

        Images.Clear();
        _allImages.Clear();
        SelectedHourFolder = null;
        IsLoading = true;
        StatusText = "Loading all images...";

        try
        {
            foreach (var hour in SelectedDateFolder.HourFolders)
            {
                var images = await _galleryService.GetImagesAsync(
                    hour.FullPath,
                    SelectedDateFolder.DisplayName,
                    hour.DisplayName
                );

                foreach (var img in images)
                {
                    var vm = new ImageItemViewModel(img, _thumbnailService);
                    _allImages.Add(vm);
                    _ = vm.LoadThumbnailAsync();
                }
            }

            FilterImages();
            StatusText = $"{_allImages.Count} images (all hours)";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Filters images based on search image text.
    /// </summary>
    private void FilterImages()
    {
        Images.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchImageText)
            ? _allImages
            : _allImages.Where(i =>
                i.FileName.Contains(SearchImageText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var img in filtered)
            Images.Add(img);
    }

    /// <summary>
    /// Filters date folders based on search text.
    /// </summary>
    private void FilterDateFolders()
    {
        DateFolders.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allDateFolders
            : _allDateFolders.Where(f =>
                f.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                f.RawName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var folder in filtered)
            DateFolders.Add(folder);
    }

    /// <summary>
    /// Opens the full-screen image viewer for the specified image.
    /// </summary>
    private async void OpenImage(ImageItemViewModel? imageVm)
    {
        if (imageVm == null) return;

        var index = Images.IndexOf(imageVm);
        if (index < 0) return;

        ViewerCurrentIndex = index;
        SelectedImage = imageVm;
        ViewerFileName = imageVm.FileName;
        ZoomLevel = 1.0;

        // Load full-resolution image
        ViewerImage = await _thumbnailService.GetFullImageAsync(imageVm.FilePath);
        UpdateViewerInfo();
        IsViewerOpen = true;
    }

    /// <summary>
    /// Closes the image viewer.
    /// </summary>
    private void CloseViewer()
    {
        IsViewerOpen = false;
        ViewerImage = null;
        ZoomLevel = 1.0;
    }

    /// <summary>
    /// Navigates to the next or previous image in the viewer.
    /// </summary>
    private async void NavigateImage(int direction)
    {
        if (Images.Count == 0) return;

        var newIndex = ViewerCurrentIndex + direction;
        if (newIndex < 0) newIndex = Images.Count - 1;
        if (newIndex >= Images.Count) newIndex = 0;

        ViewerCurrentIndex = newIndex;
        SelectedImage = Images[newIndex];
        ViewerFileName = SelectedImage.FileName;

        ViewerImage = await _thumbnailService.GetFullImageAsync(SelectedImage.FilePath);
        UpdateViewerInfo();
    }

    /// <summary>
    /// Toggles the favorite status of the currently viewed image.
    /// </summary>
    private void ToggleFavorite()
    {
        if (SelectedImage != null)
            SelectedImage.IsFavorite = !SelectedImage.IsFavorite;
    }

    /// <summary>
    /// Updates the info overlay text for the viewer.
    /// </summary>
    private void UpdateViewerInfo()
    {
        if (SelectedImage == null) return;

        var sizeStr = FormatFileSize(SelectedImage.FileSize);
        ViewerInfo = $"{SelectedImage.FileName}\n{sizeStr} • {SelectedImage.LastModified:g}";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
