using DockerManager.Core.Infrastructure;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private readonly IConfigurationService _configService;
    private bool _isDockerAvailable;
    private bool _isStartingDocker;
    private string _statusBarText = "Pronto";

    public ProjectListViewModel ProjectList { get; }
    public ProjectDetailViewModel ProjectDetail { get; }
    public LogOutputViewModel LogOutput { get; }
    public SettingsViewModel Settings { get; }
    public IConfigurationService ConfigService => _configService;

    public bool IsDockerAvailable
    {
        get => _isDockerAvailable;
        set => SetProperty(ref _isDockerAvailable, value);
    }

    public bool IsStartingDocker
    {
        get => _isStartingDocker;
        set => SetProperty(ref _isStartingDocker, value);
    }

    public string StatusBarText
    {
        get => _statusBarText;
        set => SetProperty(ref _statusBarText, value);
    }

    public AsyncRelayCommand InitializeCommand { get; }
    public AsyncRelayCommand ImportProjectCommand { get; }
    public AsyncRelayCommand ExportProjectCommand { get; }
    public AsyncRelayCommand StartDockerCommand { get; }
    public AsyncRelayCommand RefreshDockerStatusCommand { get; }

    public event Func<Task>? RequestImportProject;
    public event Func<Task>? RequestExportProject;

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
        _configService = configService;

        ProjectList.SelectedProjectChanged += project =>
        {
            ProjectDetail.CurrentProject = project;
        };

        InitializeCommand = new AsyncRelayCommand(async () =>
        {
            await configService.LoadAsync();
            ProjectList.LoadProjects();
            Settings.Load();

            await RefreshDockerStatusAsync();

            LogOutput.AddLog(IsDockerAvailable
                ? "Docker Manager avviato. Docker rilevato."
                : "Attenzione: Docker non trovato. Verifica le impostazioni.");
        });

        ImportProjectCommand = new AsyncRelayCommand(async () =>
        {
            if (RequestImportProject != null)
                await RequestImportProject.Invoke();
        });

        ExportProjectCommand = new AsyncRelayCommand(async () =>
        {
            if (RequestExportProject != null)
                await RequestExportProject.Invoke();
        });

        StartDockerCommand = new AsyncRelayCommand(async () =>
        {
            IsStartingDocker = true;
            StatusBarText = "Avvio Docker Desktop in corso...";
            LogOutput.AddLog("Tentativo di avvio Docker Desktop...");

            var started = await _dockerService.StartDockerDesktopAsync();
            if (!started)
            {
                StatusBarText = "Docker Desktop non trovato";
                LogOutput.AddLog("Impossibile avviare Docker Desktop: eseguibile non trovato.");
                IsStartingDocker = false;
                return;
            }

            // Polling: attende che Docker risponda (max ~30s)
            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(3000);
                if (await _dockerService.IsDockerAvailableAsync())
                {
                    IsDockerAvailable = true;
                    StatusBarText = "Docker disponibile";
                    LogOutput.AddLog("Docker Desktop avviato con successo.");
                    IsStartingDocker = false;
                    return;
                }
            }

            StatusBarText = "Docker avviato ma non ancora pronto - riprova tra poco";
            LogOutput.AddLog("Docker Desktop avviato ma non risponde ancora. Riprova con 'Ricontrolla'.");
            IsStartingDocker = false;
        }, () => !IsStartingDocker && !IsDockerAvailable);

        RefreshDockerStatusCommand = new AsyncRelayCommand(async () =>
        {
            await RefreshDockerStatusAsync();
            LogOutput.AddLog(IsDockerAvailable
                ? "Docker rilevato."
                : "Docker non disponibile.");
        }, () => !IsStartingDocker);
    }

    private async Task RefreshDockerStatusAsync()
    {
        IsDockerAvailable = await _dockerService.IsDockerAvailableAsync();
        StatusBarText = IsDockerAvailable
            ? "Docker disponibile"
            : "Docker non disponibile - verifica l'installazione";
        StartDockerCommand.RaiseCanExecuteChanged();
        RefreshDockerStatusCommand.RaiseCanExecuteChanged();
    }
}
