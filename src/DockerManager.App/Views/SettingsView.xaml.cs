using System.Windows;
using System.Windows.Controls;
using DockerManager.App.ViewModels;

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
            if (e.NewValue is SettingsViewModel vm && !string.IsNullOrEmpty(vm.Password))
                PasswordBox.Password = vm.Password;
        };
    }
}
