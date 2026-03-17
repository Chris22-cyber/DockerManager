using System.Collections.ObjectModel;
using System.Windows.Threading;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class ProjectDetailViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private readonly IDeploymentService _deploymentService;
    private readonly IConfigurationService _configurationService;
    private readonly LogOutputViewModel _logViewModel;
    private readonly Dispatcher _dispatcher;
    private ProjectConfig? _currentProject;
    private bool _isOperationRunning;
    private CancellationTokenSource? _cts;

    public ProjectConfig? CurrentProject
    {
        get => _currentProject;
        set
        {
            SetProperty(ref _currentProject, value);
            OnPropertyChanged(nameof(HasProject));
            OnPropertyChanged(nameof(HasDeployment));
            OnPropertyChanged(nameof(DeployServerInfo));
            RefreshImages();
            UpdateCommands();
        }
    }

    public bool HasProject => CurrentProject is not null;
    public bool HasDeployment => CurrentProject?.Deployment is not null;
    public string DeployServerInfo => CurrentProject?.Deployment is { } d ? $"{d.Host}:{d.Port}" : string.Empty;

    public bool IsOperationRunning
    {
        get => _isOperationRunning;
        set
        {
            SetProperty(ref _isOperationRunning, value);
            UpdateCommands();
        }
    }

    public ObservableCollection<SelectableImage> Images { get; } = new();

    public AsyncRelayCommand BuildAllCommand { get; }
    public AsyncRelayCommand BuildSelectedCommand { get; }
    public AsyncRelayCommand PushAllCommand { get; }
    public AsyncRelayCommand PushSelectedCommand { get; }
    public AsyncRelayCommand DeployCommand { get; }
    public AsyncRelayCommand BuildAndDeployCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand EditProjectCommand { get; }

    public event Action<ProjectConfig>? RequestEditProject;

    public ProjectDetailViewModel(
        IDockerService dockerService,
        IDeploymentService deploymentService,
        IConfigurationService configurationService,
        LogOutputViewModel logViewModel)
    {
        _dockerService = dockerService;
        _deploymentService = deploymentService;
        _configurationService = configurationService;
        _logViewModel = logViewModel;
        _dispatcher = Dispatcher.CurrentDispatcher;

        BuildAllCommand = new AsyncRelayCommand(BuildAllAsync, () => CanRunOperation());
        BuildSelectedCommand = new AsyncRelayCommand(BuildSelectedAsync, () => CanRunOperation() && HasSelectedImages());
        PushAllCommand = new AsyncRelayCommand(PushAllAsync, () => CanRunOperation());
        PushSelectedCommand = new AsyncRelayCommand(PushSelectedAsync, () => CanRunOperation() && HasSelectedImages());
        DeployCommand = new AsyncRelayCommand(DeployAsync, () => CanRunOperation() && HasDeployment);
        BuildAndDeployCommand = new AsyncRelayCommand(BuildAndDeployAsync, () => CanRunOperation() && HasDeployment);
        CancelCommand = new RelayCommand(Cancel, () => IsOperationRunning);
        EditProjectCommand = new RelayCommand(
            () => { if (CurrentProject is not null) RequestEditProject?.Invoke(CurrentProject); },
            () => CurrentProject is not null);
    }

    private bool CanRunOperation() => CurrentProject is not null && !IsOperationRunning;
    private bool HasSelectedImages() => Images.Any(i => i.IsSelected);

    private void RefreshImages()
    {
        Images.Clear();
        if (CurrentProject is null) return;

        foreach (var image in CurrentProject.Images)
        {
            var selectable = new SelectableImage(image);
            selectable.PropertyChanged += (_, _) =>
            {
                BuildSelectedCommand.RaiseCanExecuteChanged();
                PushSelectedCommand.RaiseCanExecuteChanged();
            };
            Images.Add(selectable);
        }
    }

    private void UpdateCommands()
    {
        BuildAllCommand.RaiseCanExecuteChanged();
        BuildSelectedCommand.RaiseCanExecuteChanged();
        PushAllCommand.RaiseCanExecuteChanged();
        PushSelectedCommand.RaiseCanExecuteChanged();
        DeployCommand.RaiseCanExecuteChanged();
        BuildAndDeployCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        EditProjectCommand.RaiseCanExecuteChanged();
    }

    private void Log(LogEntry entry)
    {
        _dispatcher.Invoke(() => _logViewModel.AddLog(entry));
    }

    private async Task BuildAllAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            await _dockerService.BuildAllAsync(CurrentProject, Log, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log(new LogEntry { Level = LogLevel.Warning, Message = "Build cancelled." });
        }
        finally
        {
            IsOperationRunning = false;
            _cts = null;
        }
    }

    private async Task BuildSelectedAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            var selectedImages = Images.Where(i => i.IsSelected).Select(i => i.Image).ToList();

            foreach (var image in selectedImages)
            {
                _cts.Token.ThrowIfCancellationRequested();
                foreach (var tag in image.Tags)
                {
                    var result = await _dockerService.BuildAsync(image, CurrentProject.RootDirectory, tag, CurrentProject.BuildArgs, Log, _cts.Token);
                    if (!result.Success) return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log(new LogEntry { Level = LogLevel.Warning, Message = "Build cancelled." });
        }
        finally
        {
            IsOperationRunning = false;
            _cts = null;
        }
    }

    private async Task PushAllAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            await _dockerService.PushAllAsync(CurrentProject, Log, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log(new LogEntry { Level = LogLevel.Warning, Message = "Push cancelled." });
        }
        finally
        {
            IsOperationRunning = false;
            _cts = null;
        }
    }

    private async Task PushSelectedAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            var selectedImages = Images.Where(i => i.IsSelected).Select(i => i.Image).ToList();

            foreach (var image in selectedImages)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var fullImageName = string.IsNullOrEmpty(image.Registry)
                    ? image.ImageName
                    : $"{image.Registry}/{image.ImageName}";

                foreach (var tag in image.Tags)
                {
                    var result = await _dockerService.PushAsync(fullImageName, tag, Log, _cts.Token);
                    if (!result.Success) return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log(new LogEntry { Level = LogLevel.Warning, Message = "Push cancelled." });
        }
        finally
        {
            IsOperationRunning = false;
            _cts = null;
        }
    }

    private async Task DeployAsync()
    {
        if (CurrentProject?.Deployment is not { } deployment) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            var password = _configurationService.DecryptPassword(deployment.EncryptedPassword);
            await _deploymentService.DeployAsync(deployment, password, Log, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log(new LogEntry { Level = LogLevel.Warning, Message = "Deploy annullato." });
        }
        finally
        {
            IsOperationRunning = false;
            _cts = null;
        }
    }

    private async Task BuildAndDeployAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            // 1. Build All
            Log(new LogEntry { Level = LogLevel.Info, Message = "=== Build All ===" });
            var buildResults = await _dockerService.BuildAllAsync(CurrentProject, Log, _cts.Token);
            if (buildResults.Any(r => !r.Success))
            {
                Log(new LogEntry { Level = LogLevel.Error, Message = "Build fallita. Deploy annullato." });
                return;
            }

            // 2. Push All
            Log(new LogEntry { Level = LogLevel.Info, Message = "=== Push All ===" });
            var pushResults = await _dockerService.PushAllAsync(CurrentProject, Log, _cts.Token);
            if (pushResults.Any(r => !r.Success))
            {
                Log(new LogEntry { Level = LogLevel.Error, Message = "Push fallita. Deploy annullato." });
                return;
            }

            // 3. Deploy
            if (CurrentProject.Deployment is { } deployment)
            {
                Log(new LogEntry { Level = LogLevel.Info, Message = "=== Deploy ===" });
                var password = _configurationService.DecryptPassword(deployment.EncryptedPassword);
                await _deploymentService.DeployAsync(deployment, password, Log, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Log(new LogEntry { Level = LogLevel.Warning, Message = "Build & Deploy annullato." });
        }
        finally
        {
            IsOperationRunning = false;
            _cts = null;
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
    }
}

public class SelectableImage : ViewModelBase
{
    private bool _isSelected;

    public DockerImageConfig Image { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public SelectableImage(DockerImageConfig image)
    {
        Image = image;
    }
}
