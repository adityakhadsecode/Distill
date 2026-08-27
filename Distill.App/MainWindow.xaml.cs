using Distill.App.ViewModels;
using Distill.App.Views;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinRT.Interop;

namespace Distill.App;

/// <summary>
/// Main Window for the Distill WinUI 3 application.
/// Configures Fluent 2 title bar extension, Mica backdrop, and theme-adaptive chrome.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainPage _mainPage;

    public MainWindow(MainPage mainPage)
    {
        this.InitializeComponent();
        _mainPage = mainPage;
        this.Content = mainPage;

        // 1. Extend content into titlebar
        this.ExtendsContentIntoTitleBar = true;

        // 2. Apply Mica Backdrop with fallback to DesktopAcrylic
        ApplySystemBackdrop();

        // 3. Register custom TitleBar drag region
        if (mainPage.AppTitleBar != null)
        {
            this.SetTitleBar(mainPage.AppTitleBar);
        }

        // 4. Synchronize theme and title bar button colors
        ApplyTheme(mainPage.ViewModel.SelectedTheme);
        mainPage.ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedTheme))
            {
                ApplyTheme(mainPage.ViewModel.SelectedTheme);
            }
        };
    }

    private void ApplySystemBackdrop()
    {
        if (MicaController.IsSupported())
        {
            this.SystemBackdrop = new MicaBackdrop();
        }
        else if (DesktopAcrylicController.IsSupported())
        {
            this.SystemBackdrop = new DesktopAcrylicBackdrop();
        }
    }

    public void ApplyTheme(string theme)
    {
        var elementTheme = theme switch
        {
            "Dark" => ElementTheme.Dark,
            "Light" => ElementTheme.Light,
            _ => ElementTheme.Default
        };

        _mainPage.RequestedTheme = elementTheme;
        UpdateTitleBarColors(elementTheme);
    }

    private void UpdateTitleBarColors(ElementTheme theme)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow?.TitleBar != null)
        {
            var isDark = theme == ElementTheme.Dark || (theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);

            var buttonForegroundColor = isDark ? Colors.White : Colors.Black;
            var buttonHoverBg = isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0);
            var buttonPressedBg = isDark ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(60, 0, 0, 0);

            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonForegroundColor = buttonForegroundColor;
            appWindow.TitleBar.ButtonHoverBackgroundColor = buttonHoverBg;
            appWindow.TitleBar.ButtonHoverForegroundColor = buttonForegroundColor;
            appWindow.TitleBar.ButtonPressedBackgroundColor = buttonPressedBg;
            appWindow.TitleBar.ButtonPressedForegroundColor = buttonForegroundColor;
        }
    }
}
