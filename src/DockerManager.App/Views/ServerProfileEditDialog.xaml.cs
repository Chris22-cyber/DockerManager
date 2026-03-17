using System.Windows;
using DockerManager.App.ViewModels;
using DockerManager.Core.Services;

namespace DockerManager.App.Views;

public partial class ServerProfileEditDialog : Window
{
    public ServerProfileEditDialogViewModel ViewModel => (ServerProfileEditDialogViewModel)DataContext;

    public ServerProfileEditDialog(
        IConfigurationService configurationService,
        IDeploymentService deploymentService)
    {
        InitializeComponent();
        DataContext = new ServerProfileEditDialogViewModel(configurationService, deploymentService);

        SshPasswordBox.PasswordChanged += (_, _) =>
        {
            ViewModel.Password = SshPasswordBox.Password;
        };

        RegistryPasswordBox.PasswordChanged += (_, _) =>
        {
            ViewModel.RegistryPassword = RegistryPasswordBox.Password;
        };
    }

    public void InitializePasswords()
    {
        SshPasswordBox.Password = ViewModel.Password;
        RegistryPasswordBox.Password = ViewModel.RegistryPassword;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid)
        {
            MessageBox.Show("Compila almeno il Nome e l'Host.",
                "Validazione", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
