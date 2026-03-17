using System.Collections.ObjectModel;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;

namespace DockerManager.App.ViewModels;

public class ProjectEditDialogViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _rootDirectory = string.Empty;
    private string _description = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool IsNew { get; set; } = true;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string RootDirectory
    {
        get => _rootDirectory;
        set => SetProperty(ref _rootDirectory, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public ObservableCollection<DockerImageConfig> Images { get; } = new();
    public Dictionary<string, string> BuildArgs { get; set; } = new();

    public RelayCommand AddImageCommand { get; }
    public RelayCommand RemoveImageCommand { get; }
    public RelayCommand EditImageCommand { get; }

    public event Action? RequestAddImage;
    public event Action<DockerImageConfig>? RequestEditImage;

    public ProjectEditDialogViewModel()
    {
        AddImageCommand = new RelayCommand(() =>
        {
            RequestAddImage?.Invoke();
        });

        RemoveImageCommand = new RelayCommand(param =>
        {
            if (param is DockerImageConfig image)
                Images.Remove(image);
        });

        EditImageCommand = new RelayCommand(param =>
        {
            if (param is DockerImageConfig image)
                RequestEditImage?.Invoke(image);
        });
    }

    public void LoadFrom(ProjectConfig project)
    {
        Id = project.Id;
        IsNew = false;
        Name = project.Name;
        RootDirectory = project.RootDirectory;
        Description = project.Description;
        BuildArgs = new Dictionary<string, string>(project.BuildArgs);

        Images.Clear();
        foreach (var image in project.Images)
            Images.Add(image);
    }

    public ProjectConfig ToProjectConfig() => new()
    {
        Id = Id,
        Name = Name,
        RootDirectory = RootDirectory,
        Description = Description,
        Images = Images.ToList(),
        BuildArgs = new Dictionary<string, string>(BuildArgs)
    };

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(RootDirectory);
}
