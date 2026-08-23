using Distill.App.ViewModels;
using Distill.Core.Configuration;
using Distill.Core.Downloaders;
using Distill.Core.Formatting;
using Distill.Core.Ocr;
using Distill.Core.Process;
using Distill.Core.SpeechToText;
using Distill.Core.VaultWriter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Distill.App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _mainWindow;

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application dependencies.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the application configuration root.
    /// </summary>
    public IConfiguration Configuration { get; }

    public App()
    {
        this.InitializeComponent();

        // 1. Build Configuration from appsettings.json and environment
        var baseDir = AppContext.BaseDirectory;
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        Configuration = configBuilder.Build();

        // 2. Configure Service Collection & Dependency Injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures the dependency injection container with core pipeline services, settings, and viewmodels.
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(Configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });

        // Strongly typed Settings
        services.Configure<DistillSettings>(Configuration.GetSection(DistillSettings.SectionName));

        // Subprocess and Tool Infrastructure
        services.AddSingleton<IProcessRunner, DefaultProcessRunner>();
        services.AddSingleton<IToolLocator, ToolLocator>();

        // Core Pipeline Services
        services.AddSingleton<IReelDownloader, YtDlpReelDownloader>();
        services.AddSingleton<ITextExtractor, WindowsMediaOcrExtractorStub>();
        services.AddSingleton<ITranscriber, WhisperCppTranscriberStub>();
        services.AddSingleton<INoteFormatter, OllamaNoteFormatterStub>();
        services.AddSingleton<IVaultWriter, ObsidianVaultWriterStub>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = Services.GetRequiredService<MainWindow>();
        _mainWindow.Activate();
    }
}
