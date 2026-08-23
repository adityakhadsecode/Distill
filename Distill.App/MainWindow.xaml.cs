using Distill.App.Views;
using Microsoft.UI.Xaml;

namespace Distill.App;

/// <summary>
/// Main Window for the Distill WinUI 3 application.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow(MainPage mainPage)
    {
        this.InitializeComponent();
        this.Content = mainPage;
    }
}
