using System.Windows;

namespace LumaGallery.Services;

public static class ThemeService
{
    private const string DarkThemeSource  = "Themes/DarkTheme.xaml";
    private const string LightThemeSource = "Themes/LightTheme.xaml";

    public static bool IsDark { get; private set; } = true;

    public static void Apply(bool dark)
    {
        IsDark = dark;
        var uri = new Uri(dark ? DarkThemeSource : LightThemeSource, UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };

        var merged = Application.Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(dict);
    }
}
