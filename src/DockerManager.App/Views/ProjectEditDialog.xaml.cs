using System.Windows;
using DockerManager.App.ViewModels;
using DockerManager.Core.Models;

namespace DockerManager.App.Views;

public partial class ProjectEditDialog : Window
{
    public ProjectEditDialogViewModel ViewModel => (ProjectEditDialogViewModel)DataContext;

    public ProjectEditDialog()
    {
        InitializeComponent();
        DataContext = new ProjectEditDialogViewModel();

        ViewModel.RequestAddImage += ShowAddImageDialog;
        ViewModel.RequestEditImage += ShowEditImageDialog;
    }

    private void ShowAddImageDialog()
    {
        var dialog = new ImageEditDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.Images.Add(dialog.ViewModel.ToDockerImageConfig());
        }
    }

    private void ShowEditImageDialog(DockerImageConfig image)
    {
        var dialog = new ImageEditDialog { Owner = this };
        dialog.ViewModel.LoadFrom(image);
        if (dialog.ShowDialog() == true)
        {
            var updated = dialog.ViewModel.ToDockerImageConfig();
            var index = -1;
            for (int i = 0; i < ViewModel.Images.Count; i++)
            {
                if (ViewModel.Images[i].Id == updated.Id)
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
                ViewModel.Images[index] = updated;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsValid)
        {
            MessageBox.Show("Compila tutti i campi obbligatori (Nome, Root Directory).",
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

    private void BrowseRootDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Seleziona Root Directory del progetto"
        };
        if (dlg.ShowDialog() == true)
        {
            ViewModel.RootDirectory = dlg.FolderName;
        }
    }
}
