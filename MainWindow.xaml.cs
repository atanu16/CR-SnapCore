using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LumaGallery.Models;
using LumaGallery.ViewModels;

namespace LumaGallery;

/// <summary>
/// MainWindow code-behind. Handles window chrome, keyboard shortcuts,
/// and animation triggers that can't be done purely in XAML.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    #region Window Chrome (Custom Title Bar)

    /// <summary>
    /// Enables dragging the window from the custom title bar areas.
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click toggles maximize
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private bool _isAnimatedMaximized;
    private Rect _restoreBounds;

    private void ToggleMaximize()
    {
        if (_isAnimatedMaximized)
            AnimateRestore();
        else
            AnimateMaximize();
    }

    private void AnimateMaximize()
    {
        _restoreBounds = new Rect(Left, Top, Width, Height);

        var workArea = SystemParameters.WorkArea;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(260);

        AnimateWindowProp(LeftProperty, workArea.Left, duration, ease);
        AnimateWindowProp(TopProperty, workArea.Top, duration, ease);
        AnimateWindowProp(WidthProperty, workArea.Width, duration, ease);
        AnimateWindowProp(HeightProperty, workArea.Height, duration, ease);

        _isAnimatedMaximized = true;
    }

    private void AnimateRestore()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(260);

        AnimateWindowProp(LeftProperty, _restoreBounds.X, duration, ease);
        AnimateWindowProp(TopProperty, _restoreBounds.Y, duration, ease);
        AnimateWindowProp(WidthProperty, _restoreBounds.Width, duration, ease);
        AnimateWindowProp(HeightProperty, _restoreBounds.Height, duration, ease);

        _isAnimatedMaximized = false;
    }

    private void AnimateWindowProp(DependencyProperty prop, double to, TimeSpan duration, IEasingFunction ease)
    {
        var anim = new DoubleAnimation
        {
            To = to,
            Duration = duration,
            EasingFunction = ease,
            FillBehavior = FillBehavior.Stop
        };
        anim.Completed += (_, _) =>
        {
            BeginAnimation(prop, null);
            SetValue(prop, to);
        };
        BeginAnimation(prop, anim);
    }

    #endregion

    #region Keyboard Shortcuts

    /// <summary>
    /// Global keyboard handler for navigation and shortcuts.
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Image viewer shortcuts
        if (ViewModel.IsViewerOpen)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    ViewModel.CloseViewerCommand.Execute(null);
                    break;
                case Key.Left:
                    ViewModel.PrevImageCommand.Execute(null);
                    break;
                case Key.Right:
                    ViewModel.NextImageCommand.Execute(null);
                    break;
                case Key.OemPlus:
                case Key.Add:
                    ViewModel.ZoomInCommand.Execute(null);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ViewModel.ZoomOutCommand.Execute(null);
                    break;
                case Key.D0:
                case Key.NumPad0:
                    ViewModel.ResetZoomCommand.Execute(null);
                    break;
                case Key.F:
                    ViewModel.ToggleFavoriteCommand.Execute(null);
                    break;
                case Key.I:
                    ViewModel.ToggleInfoCommand.Execute(null);
                    break;
            }
        }
        else
        {
            // Gallery shortcuts
            switch (e.Key)
            {
                case Key.F5:
                    ViewModel.RefreshCommand.Execute(null);
                    break;
            }
        }
    }

    #endregion

    #region Hour Tab Selection

    /// <summary>
    /// Handles hour tab click to update the ViewModel's selected hour.
    /// RadioButtons in an ItemsControl need manual binding for selection.
    /// </summary>
    private void HourTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn &&
            btn.Tag is HourFolder hour)
        {
            ViewModel.SelectedHourFolder = hour;
        }
    }

    #endregion

    #region Image Card Animation

    /// <summary>
    /// Triggers a staggered fade-in animation when image cards load.
    /// Each card gets a slight delay based on its position for a cascade effect.
    /// </summary>
    private void ImageCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            // Get index for stagger delay
            var itemsControl = GalleryItemsControl;
            var index = 0;
            if (element.DataContext != null && itemsControl?.ItemsSource != null)
            {
                foreach (var item in itemsControl.ItemsSource)
                {
                    if (item == element.DataContext) break;
                    index++;
                }
            }

            // Cap stagger to prevent very long delays on large collections
            var staggerMs = Math.Min(index * 30, 600);

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(staggerMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slideUp = new DoubleAnimation
            {
                From = 24,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(staggerMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(OpacityProperty, fadeIn);

            if (element.RenderTransform is System.Windows.Media.TranslateTransform transform)
            {
                if (transform.IsFrozen)
                {
                    transform = transform.Clone();
                    element.RenderTransform = transform;
                }
                transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
            }
        }
    }

    #endregion

    #region Viewer Interactions

    /// <summary>
    /// Mouse wheel zoom in the image viewer.
    /// </summary>
    private void Viewer_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel.IsViewerOpen)
        {
            if (e.Delta > 0)
                ViewModel.ZoomInCommand.Execute(null);
            else
                ViewModel.ZoomOutCommand.Execute(null);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Click on viewer background to close (except on controls).
    /// </summary>
    private void ViewerBg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Only close if clicking the background overlay directly
        if (e.OriginalSource == sender)
        {
            ViewModel.CloseViewerCommand.Execute(null);
        }
    }

    #endregion
}