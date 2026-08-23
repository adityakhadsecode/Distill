using Distill.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Distill.App.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage(MainViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
    }

    private void MainNavView_Loaded(object sender, RoutedEventArgs e)
    {
        ShowView("Queue");
    }

    private void MainNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            ShowView(tag);
        }
    }

    private void ShowView(string viewTag)
    {
        if (viewTag == "Settings")
        {
            QueueViewPanel.Visibility = Visibility.Collapsed;
            SettingsViewPanel.Visibility = Visibility.Visible;
        }
        else
        {
            QueueViewPanel.Visibility = Visibility.Visible;
            SettingsViewPanel.Visibility = Visibility.Collapsed;
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
}
