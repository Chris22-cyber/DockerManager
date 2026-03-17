using System.Collections.ObjectModel;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class ProjectListViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private ProjectConfig? _selectedProject;

    public ObservableCollection<ProjectConfig> Projects { get; } = new();

    public ProjectConfig? SelectedProject
    {
        get => _selectedProject;
        set => SetProperty(ref _selectedProject, value);
    }

    public event Action<ProjectConfig?>? SelectedProjectChanged;

    public RelayCommand AddProjectCommand { get; }
    public RelayCommand RemoveProjectCommand { get; }

    public ProjectListViewModel(IConfigurationService configService)
    {
        _configService = configService;

        AddProjectCommand = new RelayCommand(() =>
        {
            RequestAddProject?.Invoke();
        });

        RemoveProjectCommand = new RelayCommand(
            () =>
            {
                if (SelectedProject is null) return;
                _configService.RemoveProject(SelectedProject.Id);
                Projects.Remove(SelectedProject);
                SelectedProject = Projects.FirstOrDefault();
                _ = _configService.SaveAsync();
            },
            () => SelectedProject is not null);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedProject))
            {
                SelectedProjectChanged?.Invoke(SelectedProject);
                RemoveProjectCommand.RaiseCanExecuteChanged();
            }
        };
    }

    public event Action? RequestAddProject;

    public void LoadProjects()
    {
        Projects.Clear();
        foreach (var project in _configService.Configuration.Projects)
            Projects.Add(project);

        SelectedProject = Projects.FirstOrDefault();
    }

    public void AddProject(ProjectConfig project)
    {
        _configService.AddProject(project);
        Projects.Add(project);
        SelectedProject = project;
        _ = _configService.SaveAsync();
    }

    public void UpdateProject(ProjectConfig project)
    {
        _configService.UpdateProject(project);
        var index = -1;
        for (int i = 0; i < Projects.Count; i++)
        {
            if (Projects[i].Id == project.Id)
            {
                index = i;
                break;
            }
        }
        if (index >= 0)
        {
            Projects[index] = project;
            SelectedProject = project;
        }
        _ = _configService.SaveAsync();
    }
}
