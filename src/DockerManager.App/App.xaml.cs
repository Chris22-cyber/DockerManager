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
    public IServiceProvider Services => _serviceProvider!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Errore durante l'avvio:\n\n{ex.Message}\n\n{ex.InnerException?.Message}\n\nStack:\n{ex.StackTrace}",
                "Docker Manager - Errore Avvio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        // Services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IDockerService, DockerService>();
        services.AddSingleton<IDockerfileDiscoveryService, DockerfileDiscoveryService>();
        services.AddSingleton<IDeploymentService, DeploymentService>();

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
