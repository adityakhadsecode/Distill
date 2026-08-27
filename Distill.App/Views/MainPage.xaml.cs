using Distill.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using WinRT.Interop;

namespace Distill.App.Views;

/// <summary>
/// Main interactive page for Distill, hosting the Fluent 2 NavigationView, Extraction Queue,
/// Get Started onboarding guide, and Settings & Diagnostics.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public UIElement? AppTitleBar => TitleBarGrid;

    public MainPage(MainViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;

        // Synchronize NavigationView selection when ViewModel changes view
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentViewTag))
            {
                SyncNavViewSelection(ViewModel.CurrentViewTag);
            }
        };
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        SyncNavViewSelection(ViewModel.CurrentViewTag);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ViewModel.NavigateTo("Settings");
        }
        else if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString() ?? "Extract";
            ViewModel.NavigateTo(tag);
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ViewModel.NavigateTo("Settings");
        }
        else if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString() ?? "Extract";
            ViewModel.NavigateTo(tag);
        }
    }

    private void SyncNavViewSelection(string tag)
    {
        if (NavView == null) return;

        if (tag == "Settings")
        {
            NavView.SelectedItem = NavView.SettingsItem;
        }
        else if (tag == "Onboarding")
        {
            NavView.SelectedItem = OnboardingNavItem;
        }
        else
        {
            NavView.SelectedItem = ExtractNavItem;
        }
    }

    private void UrlInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.AddJobCommand.CanExecute(null))
        {
            ViewModel.AddJobCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void BrowseVaultFolder_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = App.Current.MainWindow != null ? WindowNative.GetWindowHandle(App.Current.MainWindow) : nint.Zero;
        await ViewModel.BrowseVaultFolderAsync(hwnd);
    }

    private async void BrowseWhisperModel_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = App.Current.MainWindow != null ? WindowNative.GetWindowHandle(App.Current.MainWindow) : nint.Zero;
        await ViewModel.BrowseWhisperModelFileAsync(hwnd);
    }

    private async void BrowseYtDlp_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = App.Current.MainWindow != null ? WindowNative.GetWindowHandle(App.Current.MainWindow) : nint.Zero;
        await ViewModel.BrowseYtDlpBinaryFileAsync(hwnd);
    }

    private async void BrowseFfmpeg_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = App.Current.MainWindow != null ? WindowNative.GetWindowHandle(App.Current.MainWindow) : nint.Zero;
        await ViewModel.BrowseFfmpegBinaryFileAsync(hwnd);
    }

    private async void BrowseWhisperBinary_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = App.Current.MainWindow != null ? WindowNative.GetWindowHandle(App.Current.MainWindow) : nint.Zero;
        await ViewModel.BrowseWhisperBinaryFileAsync(hwnd);
    }
}
