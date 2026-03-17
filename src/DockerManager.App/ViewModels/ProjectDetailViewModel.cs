using System.Collections.ObjectModel;
using System.Windows.Threading;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class ProjectDetailViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
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
            RefreshImages();
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

    public ObservableCollection<SelectableImage> Images { get; } = new();

    public AsyncRelayCommand BuildAllCommand { get; }
    public AsyncRelayCommand BuildSelectedCommand { get; }
    public AsyncRelayCommand PushAllCommand { get; }
    public AsyncRelayCommand PushSelectedCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand EditProjectCommand { get; }

    public event Action<ProjectConfig>? RequestEditProject;

    public ProjectDetailViewModel(
        IDockerService dockerService,
        LogOutputViewModel logViewModel)
    {
        _dockerService = dockerService;
        _logViewModel = logViewModel;
        _dispatcher = Dispatcher.CurrentDispatcher;

        BuildAllCommand = new AsyncRelayCommand(BuildAllAsync, () => CanRunOperation());
        BuildSelectedCommand = new AsyncRelayCommand(BuildSelectedAsync, () => CanRunOperation() && HasSelectedImages());
        PushAllCommand = new AsyncRelayCommand(PushAllAsync, () => CanRunOperation());
        PushSelectedCommand = new AsyncRelayCommand(PushSelectedAsync, () => CanRunOperation() && HasSelectedImages());
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
