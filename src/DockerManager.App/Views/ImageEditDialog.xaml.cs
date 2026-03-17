using System.Windows;
using DockerManager.App.ViewModels;
using Microsoft.Win32;

namespace DockerManager.App.Views;

public partial class ImageEditDialog : Window
{
    public ImageEditDialogViewModel ViewModel => (ImageEditDialogViewModel)DataContext;

    public ImageEditDialog()
    {
        InitializeComponent();
        DataContext = new ImageEditDialogViewModel();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid)
        {
            MessageBox.Show("Compila tutti i campi obbligatori (Nome, Dockerfile, Nome Immagine Docker).",
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
