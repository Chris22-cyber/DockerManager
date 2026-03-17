using System.Windows;
using DockerManager.App.ViewModels;
using DockerManager.Core.Models;

namespace DockerManager.App.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            viewModel.InitializeCommand.Execute(null);
        };

        viewModel.ProjectList.RequestAddProject += ShowAddProjectDialog;
        viewModel.ProjectDetail.RequestEditProject += ShowEditProjectDialog;
    }

    private void ShowAddProjectDialog()
    {
        var dialog = new ProjectEditDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.ProjectList.AddProject(dialog.ViewModel.ToProjectConfig());
        }
    }

    private void ShowEditProjectDialog(ProjectConfig project)
    {
        var dialog = new ProjectEditDialog { Owner = this };
        dialog.ViewModel.LoadFrom(project);
        if (dialog.ShowDialog() == true)
        {
            ViewModel.ProjectList.UpdateProject(dialog.ViewModel.ToProjectConfig());
        }
    }

    private void NewProject_Click(object sender, RoutedEventArgs e) => ShowAddProjectDialog();

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Visible;
        MainContent.Visibility = Visibility.Collapsed;
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
        MainContent.Visibility = Visibility.Visible;
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Docker Manager v1.0.0\nGestore operazioni Docker per Windows",
            "Informazioni", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
