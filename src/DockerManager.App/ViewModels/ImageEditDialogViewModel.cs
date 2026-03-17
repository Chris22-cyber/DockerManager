using System.Collections.ObjectModel;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;

namespace DockerManager.App.ViewModels;

public class ImageEditDialogViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _dockerfilePath = string.Empty;
    private string _contextDirectory = string.Empty;
    private string _imageName = string.Empty;
    private string _registry = string.Empty;
    private string _newTag = string.Empty;
    private string _newDeployCommand = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool IsNew { get; set; } = true;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string DockerfilePath
    {
        get => _dockerfilePath;
        set => SetProperty(ref _dockerfilePath, value);
    }

    public string ContextDirectory
    {
        get => _contextDirectory;
        set => SetProperty(ref _contextDirectory, value);
    }

    public string ImageName
    {
        get => _imageName;
        set => SetProperty(ref _imageName, value);
    }

    public string Registry
    {
        get => _registry;
        set => SetProperty(ref _registry, value);
    }

    public string NewTag
    {
        get => _newTag;
        set => SetProperty(ref _newTag, value);
    }

    public string NewDeployCommand
    {
        get => _newDeployCommand;
        set => SetProperty(ref _newDeployCommand, value);
    }

    public ObservableCollection<string> Tags { get; } = new() { "latest" };
    public ObservableCollection<string> DeployCommands { get; } = new();
    public Dictionary<string, string> BuildArgs { get; set; } = new();

    public RelayCommand AddTagCommand { get; }
    public RelayCommand RemoveTagCommand { get; }
    public RelayCommand AddDeployCommandCommand { get; }
    public RelayCommand RemoveDeployCommandCommand { get; }

    public ImageEditDialogViewModel()
    {
        AddTagCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrWhiteSpace(NewTag) && !Tags.Contains(NewTag))
            {
                Tags.Add(NewTag);
                NewTag = string.Empty;
            }
        });

        RemoveTagCommand = new RelayCommand(param =>
        {
            if (param is string tag)
                Tags.Remove(tag);
        });

        AddDeployCommandCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrWhiteSpace(NewDeployCommand))
            {
                DeployCommands.Add(NewDeployCommand.Trim());
                NewDeployCommand = string.Empty;
            }
        });

        RemoveDeployCommandCommand = new RelayCommand(param =>
        {
            if (param is string cmd)
                DeployCommands.Remove(cmd);
        });
    }

    public void LoadFrom(DockerImageConfig image)
    {
        Id = image.Id;
        IsNew = false;
        Name = image.Name;
        DockerfilePath = image.DockerfilePath;
        ContextDirectory = image.ContextDirectory;
        ImageName = image.ImageName;
        Registry = image.Registry;
        BuildArgs = new Dictionary<string, string>(image.BuildArgs);

        Tags.Clear();
        foreach (var tag in image.Tags)
            Tags.Add(tag);

        DeployCommands.Clear();
        foreach (var cmd in image.DeployCommands)
            DeployCommands.Add(cmd);
    }

    public DockerImageConfig ToDockerImageConfig() => new()
    {
        Id = Id,
        Name = Name,
        DockerfilePath = DockerfilePath,
        ContextDirectory = ContextDirectory,
        ImageName = ImageName,
        Registry = Registry,
        Tags = Tags.ToList(),
        BuildArgs = new Dictionary<string, string>(BuildArgs),
        DeployCommands = DeployCommands.ToList()
    };

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(DockerfilePath) &&
        !string.IsNullOrWhiteSpace(ImageName);
}
