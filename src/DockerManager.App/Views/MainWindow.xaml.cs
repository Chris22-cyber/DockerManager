using System.Windows;
using DockerManager.App.ViewModels;
using DockerManager.Core.Models;
using Microsoft.Win32;

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

    private async void ImportProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Importa Progetto",
            Filter = "File JSON|*.json|Tutti i file|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                await ViewModel.ConfigService.ImportProjectAsync(dlg.FileName);
                await ViewModel.ConfigService.SaveAsync();
                ViewModel.ProjectList.LoadProjects();
                MessageBox.Show("Progetto importato con successo.", "Importa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'importazione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ExportProject_Click(object sender, RoutedEventArgs e)
    {
        var selectedProject = ViewModel.ProjectDetail.CurrentProject;
        if (selectedProject is null)
        {
            MessageBox.Show("Seleziona un progetto da esportare.", "Esporta", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Esporta Progetto",
            Filter = "File JSON|*.json",
            FileName = $"{selectedProject.Name}.json"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                await ViewModel.ConfigService.ExportProjectAsync(selectedProject.Id, dlg.FileName);
                MessageBox.Show("Progetto esportato con successo.", "Esporta", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'esportazione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

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
        MessageBox.Show("Docker Manager v2.0.0\nGestore operazioni Docker per Windows\nSupporto multi-immagine per progetto",
            "Informazioni", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
