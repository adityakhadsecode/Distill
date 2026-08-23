using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace Distill.App;

/// <summary>
/// Custom entry point required for WinUI 3 unpackaged applications.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}
