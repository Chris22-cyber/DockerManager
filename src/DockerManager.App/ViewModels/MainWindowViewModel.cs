using DockerManager.Core.Infrastructure;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private bool _isDockerAvailable;
    private string _statusBarText = "Pronto";

    public ProjectListViewModel ProjectList { get; }
    public ProjectDetailViewModel ProjectDetail { get; }
    public LogOutputViewModel LogOutput { get; }
    public SettingsViewModel Settings { get; }

    public bool IsDockerAvailable
    {
        get => _isDockerAvailable;
        set => SetProperty(ref _isDockerAvailable, value);
    }

    public string StatusBarText
    {
        get => _statusBarText;
        set => SetProperty(ref _statusBarText, value);
    }

    public AsyncRelayCommand InitializeCommand { get; }

    public MainWindowViewModel(
        ProjectListViewModel projectList,
        ProjectDetailViewModel projectDetail,
        LogOutputViewModel logOutput,
        SettingsViewModel settings,
        IDockerService dockerService,
        IConfigurationService configService)
    {
        ProjectList = projectList;
        ProjectDetail = projectDetail;
        LogOutput = logOutput;
        Settings = settings;
        _dockerService = dockerService;

        ProjectList.SelectedProjectChanged += project =>
        {
            ProjectDetail.CurrentProject = project;
        };

        InitializeCommand = new AsyncRelayCommand(async () =>
        {
            await configService.LoadAsync();
            ProjectList.LoadProjects();
            Settings.Load();

            IsDockerAvailable = await _dockerService.IsDockerAvailableAsync();
            StatusBarText = IsDockerAvailable
                ? "Docker disponibile"
                : "Docker non disponibile - verifica l'installazione";

            LogOutput.AddLog(IsDockerAvailable
                ? "Docker Manager avviato. Docker rilevato."
                : "Attenzione: Docker non trovato. Verifica le impostazioni.");
        });
    }
}
