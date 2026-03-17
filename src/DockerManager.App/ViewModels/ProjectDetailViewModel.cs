using System.Windows.Threading;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class ProjectDetailViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private readonly ITagGeneratorService _tagService;
    private readonly LogOutputViewModel _logViewModel;
    private readonly Dispatcher _dispatcher;
    private ProjectConfig? _currentProject;
    private bool _isOperationRunning;
    private string _selectedTag = "latest";
    private CancellationTokenSource? _cts;

    public ProjectConfig? CurrentProject
    {
        get => _currentProject;
        set
        {
            SetProperty(ref _currentProject, value);
            OnPropertyChanged(nameof(HasProject));
            UpdateCommands();
        }
    }

    public bool HasProject => CurrentProject is not null;

    public bool IsOperationRunning
    {
        get => _isOperationRunning;
        set
        {
            SetProperty(ref _isOperationRunning, value);
            UpdateCommands();
        }
    }

    public string SelectedTag
    {
        get => _selectedTag;
        set => SetProperty(ref _selectedTag, value);
    }

    public AsyncRelayCommand BuildCommand { get; }
    public AsyncRelayCommand TagCommand { get; }
    public AsyncRelayCommand PushCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand EditProjectCommand { get; }

    public event Action<ProjectConfig>? RequestEditProject;

    public ProjectDetailViewModel(
        IDockerService dockerService,
        ITagGeneratorService tagService,
        LogOutputViewModel logViewModel)
    {
        _dockerService = dockerService;
        _tagService = tagService;
        _logViewModel = logViewModel;
        _dispatcher = Dispatcher.CurrentDispatcher;

        BuildCommand = new AsyncRelayCommand(BuildAsync, () => CanRunOperation());
        TagCommand = new AsyncRelayCommand(TagAsync, () => CanRunOperation());
        PushCommand = new AsyncRelayCommand(PushAsync, () => CanRunOperation());
        CancelCommand = new RelayCommand(Cancel, () => IsOperationRunning);
        EditProjectCommand = new RelayCommand(
            () => { if (CurrentProject is not null) RequestEditProject?.Invoke(CurrentProject); },
            () => CurrentProject is not null);
    }

    private bool CanRunOperation() => CurrentProject is not null && !IsOperationRunning;

    private void UpdateCommands()
    {
        BuildCommand.RaiseCanExecuteChanged();
        TagCommand.RaiseCanExecuteChanged();
        PushCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        EditProjectCommand.RaiseCanExecuteChanged();
    }

    private void Log(LogEntry entry)
    {
        _dispatcher.Invoke(() => _logViewModel.AddLog(entry));
    }

    private async Task BuildAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            var tag = string.IsNullOrEmpty(SelectedTag) ? "latest" : SelectedTag;
            await _dockerService.BuildAsync(CurrentProject, tag, Log, _cts.Token);
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

    private async Task TagAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            foreach (var tag in CurrentProject.DefaultTags)
            {
                var source = _tagService.GetFullImageName(CurrentProject.Registry, CurrentProject.ImageName, "latest");
                var target = _tagService.GetFullImageName(CurrentProject.Registry, CurrentProject.ImageName, tag);
                await _dockerService.TagAsync(source, target, Log, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Log(new LogEntry { Level = LogLevel.Warning, Message = "Tag operation cancelled." });
        }
        finally
        {
            IsOperationRunning = false;
            _cts = null;
        }
    }

    private async Task PushAsync()
    {
        if (CurrentProject is null) return;

        _cts = new CancellationTokenSource();
        IsOperationRunning = true;

        try
        {
            var image = string.IsNullOrEmpty(CurrentProject.Registry)
                ? CurrentProject.ImageName
                : $"{CurrentProject.Registry}/{CurrentProject.ImageName}";

            foreach (var tag in CurrentProject.DefaultTags)
            {
                await _dockerService.PushAsync(image, tag, Log, _cts.Token);
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

    private void Cancel()
    {
        _cts?.Cancel();
    }
}
