using System.Windows;
using DockerManager.App.ViewModels;
using DockerManager.App.Views;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DockerManager.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        // Services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IDockerService, DockerService>();
        services.AddSingleton<IDockerfileDiscoveryService, DockerfileDiscoveryService>();
        services.AddSingleton<ITagGeneratorService, TagGeneratorService>();

        // ViewModels
        services.AddSingleton<LogOutputViewModel>();
        services.AddSingleton<ProjectListViewModel>();
        services.AddSingleton<ProjectDetailViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
