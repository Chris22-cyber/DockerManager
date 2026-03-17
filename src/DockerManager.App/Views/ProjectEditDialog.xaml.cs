using System.Windows;
using DockerManager.App.ViewModels;
using Microsoft.Win32;

namespace DockerManager.App.Views;

public partial class ProjectEditDialog : Window
{
    public ProjectEditDialogViewModel ViewModel => (ProjectEditDialogViewModel)DataContext;

    public ProjectEditDialog()
    {
        InitializeComponent();
        DataContext = new ProjectEditDialogViewModel();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid)
        {
            MessageBox.Show("Compila tutti i campi obbligatori (Nome, Dockerfile, Directory Contesto, Nome Immagine).",
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

    private void BrowseDockerfile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Seleziona Dockerfile",
            Filter = "Dockerfile|Dockerfile;Dockerfile.*|Tutti i file|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            ViewModel.DockerfilePath = dlg.FileName;

            if (string.IsNullOrEmpty(ViewModel.ContextDirectory))
                ViewModel.ContextDirectory = System.IO.Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
        }
    }

    private void BrowseContext_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Seleziona directory contesto"
        };
        if (dlg.ShowDialog() == true)
        {
            ViewModel.ContextDirectory = dlg.FolderName;
        }
    }
}
