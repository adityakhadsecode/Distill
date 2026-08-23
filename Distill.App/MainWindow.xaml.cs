using Distill.App.ViewModels;
using Microsoft.UI.Xaml;

namespace Distill.App;

/// <summary>
/// Main Window for the Distill WinUI 3 application.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
    }
}
