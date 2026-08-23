using Distill.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Distill.App.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage(MainViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
    }
}
