using System.Windows;
using System.Windows.Controls;
using DockerManager.App.ViewModels;
using DockerManager.Core.Services;

namespace DockerManager.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        PasswordBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
                vm.Password = PasswordBox.Password;
        };

        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is SettingsViewModel vm)
            {
                if (!string.IsNullOrEmpty(vm.Password))
                    PasswordBox.Password = vm.Password;

                vm.RequestAddProfile += OnAddProfile;
                vm.RequestEditProfile += OnEditProfile;
            }

            if (e.OldValue is SettingsViewModel oldVm)
            {
                oldVm.RequestAddProfile -= OnAddProfile;
                oldVm.RequestEditProfile -= OnEditProfile;
            }
        };
    }

    private SettingsViewModel Vm => (SettingsViewModel)DataContext;

    private void OnAddProfile()
    {
        var owner = Window.GetWindow(this);
        var configService = GetConfigService();
        var deployService = GetDeployService();
        if (configService is null || deployService is null) return;

        var dialog = new ServerProfileEditDialog(configService, deployService) { Owner = owner };
        if (dialog.ShowDialog() == true)
        {
            var profile = dialog.ViewModel.ToServerProfile();
            configService.AddProfile(profile);
            _ = configService.SaveAsync();
            Vm.RefreshProfiles();
        }
    }

    private void OnEditProfile(Core.Models.ServerProfile profile)
    {
        var owner = Window.GetWindow(this);
        var configService = GetConfigService();
        var deployService = GetDeployService();
        if (configService is null || deployService is null) return;

        var dialog = new ServerProfileEditDialog(configService, deployService) { Owner = owner };
        dialog.ViewModel.LoadFrom(profile);
        dialog.InitializePasswords();
        if (dialog.ShowDialog() == true)
        {
            var updated = dialog.ViewModel.ToServerProfile();
            configService.UpdateProfile(updated);
            _ = configService.SaveAsync();
            Vm.RefreshProfiles();
        }
    }

    private IConfigurationService? GetConfigService()
    {
        // Walk up to MainWindow to get the service from its ViewModel
        if (Window.GetWindow(this) is MainWindow mainWindow &&
            mainWindow.DataContext is MainWindowViewModel mainVm)
        {
            return mainVm.ConfigService;
        }
        return null;
    }

    private IDeploymentService? GetDeployService()
    {
        // Use the App's service provider
        if (Application.Current is App app)
        {
            return app.Services.GetService(typeof(IDeploymentService)) as IDeploymentService;
        }
        return null;
    }
}
