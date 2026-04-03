namespace LumaGallery.ViewModels;

/// <summary>
/// Represents a named image group filter derived from the filename prefix before '_'.
/// </summary>
public class ImageGroup : ViewModelBase
{
    /// <summary>Empty string means "All images".</summary>
    public string Name { get; }
    public string DisplayName => string.IsNullOrEmpty(Name) ? "All" : Name;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ImageGroup(string name) => Name = name;
}
