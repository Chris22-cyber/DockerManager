using System.Collections.ObjectModel;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class ProjectEditDialogViewModel : ViewModelBase
{
    private readonly IConfigurationService? _configService;
    private readonly IDeploymentService? _deploymentService;

    private string _name = string.Empty;
    private string _rootDirectory = string.Empty;
    private string _description = string.Empty;

    // Deployment SSH fields
    private string _deployHost = string.Empty;
    private int _deployPort = 22;
    private string _deployUsername = string.Empty;
    private string _deployPassword = string.Empty;
    private string _deployWorkingDirectory = string.Empty;
    private string _testConnectionStatus = string.Empty;
    private bool _isTestingConnection;
    private bool _deployUseSudo;

    // Server Profile
    private ServerProfile? _selectedProfile;
    private bool _isUsingProfile;

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
    public ObservableCollection<ServerProfile> AvailableProfiles { get; } = new();

    public ServerProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                IsUsingProfile = value is not null;
                if (value is not null)
                {
                    DeployHost = value.Host;
                    DeployPort = value.Port;
                    DeployUsername = value.Username;
                    DeployWorkingDirectory = value.WorkingDirectory;
                    DeployUseSudo = value.UseSudo;
                    DeployPassword = _configService is not null && !string.IsNullOrEmpty(value.EncryptedPassword)
                        ? _configService.DecryptPassword(value.EncryptedPassword)
                        : string.Empty;
                }
            }
        }
    }

    public bool IsUsingProfile
    {
        get => _isUsingProfile;
        set
        {
            if (SetProperty(ref _isUsingProfile, value))
                OnPropertyChanged(nameof(IsManualConfig));
        }
    }

    public bool IsManualConfig => !IsUsingProfile;

    // Deployment SSH properties
    public string DeployHost
    {
        get => _deployHost;
        set => SetProperty(ref _deployHost, value);
    }

    public int DeployPort
    {
        get => _deployPort;
        set => SetProperty(ref _deployPort, value);
    }

    public string DeployUsername
    {
        get => _deployUsername;
        set => SetProperty(ref _deployUsername, value);
    }

    public string DeployPassword
    {
        get => _deployPassword;
        set => SetProperty(ref _deployPassword, value);
    }

    public string DeployWorkingDirectory
    {
        get => _deployWorkingDirectory;
        set => SetProperty(ref _deployWorkingDirectory, value);
    }

    public bool DeployUseSudo
    {
        get => _deployUseSudo;
        set => SetProperty(ref _deployUseSudo, value);
    }

    public string TestConnectionStatus
    {
        get => _testConnectionStatus;
        set => SetProperty(ref _testConnectionStatus, value);
    }

    public bool IsTestingConnection
    {
        get => _isTestingConnection;
        set => SetProperty(ref _isTestingConnection, value);
    }

    public RelayCommand AddImageCommand { get; }
    public RelayCommand RemoveImageCommand { get; }
    public RelayCommand EditImageCommand { get; }
    public AsyncRelayCommand TestConnectionCommand { get; }

    public event Action? RequestAddImage;
    public event Action<DockerImageConfig>? RequestEditImage;

    public ProjectEditDialogViewModel()
        : this(null, null)
    {
    }

    public ProjectEditDialogViewModel(
        IConfigurationService? configService,
        IDeploymentService? deploymentService)
    {
        _configService = configService;
        _deploymentService = deploymentService;

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

        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync,
            () => !IsTestingConnection && !string.IsNullOrWhiteSpace(DeployHost));
    }

    private async Task TestConnectionAsync()
    {
        if (_deploymentService is null || string.IsNullOrWhiteSpace(DeployHost))
            return;

        IsTestingConnection = true;
        TestConnectionStatus = "Connessione in corso...";
        TestConnectionCommand.RaiseCanExecuteChanged();

        try
        {
            var config = new DeploymentConfig
            {
                Host = DeployHost,
                Port = DeployPort,
                Username = DeployUsername,
                WorkingDirectory = DeployWorkingDirectory
            };

            var success = await _deploymentService.TestConnectionAsync(config, DeployPassword);
            TestConnectionStatus = success
                ? "Connessione riuscita!"
                : "Connessione fallita.";
        }
        catch (Exception ex)
        {
            TestConnectionStatus = $"Errore: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
            TestConnectionCommand.RaiseCanExecuteChanged();
        }
    }

    public void LoadProfiles()
    {
        AvailableProfiles.Clear();
        if (_configService is null) return;
        foreach (var profile in _configService.GetAllProfiles())
            AvailableProfiles.Add(profile);
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

        LoadProfiles();

        // Load profile or legacy deployment config
        if (!string.IsNullOrEmpty(project.ServerProfileName))
        {
            SelectedProfile = AvailableProfiles
                .FirstOrDefault(p => string.Equals(p.Name, project.ServerProfileName, StringComparison.OrdinalIgnoreCase));
        }
        else if (project.Deployment is { } deployment)
        {
            DeployHost = deployment.Host;
            DeployPort = deployment.Port;
            DeployUsername = deployment.Username;
            DeployWorkingDirectory = deployment.WorkingDirectory;
            DeployPassword = _configService is not null && !string.IsNullOrEmpty(deployment.EncryptedPassword)
                ? _configService.DecryptPassword(deployment.EncryptedPassword)
                : string.Empty;
            DeployUseSudo = deployment.UseSudo;
        }
    }

    public ProjectConfig ToProjectConfig()
    {
        var config = new ProjectConfig
        {
            Id = Id,
            Name = Name,
            RootDirectory = RootDirectory,
            Description = Description,
            Images = Images.ToList(),
            BuildArgs = new Dictionary<string, string>(BuildArgs)
        };

        if (IsUsingProfile && SelectedProfile is not null)
        {
            config.ServerProfileName = SelectedProfile.Name;
            config.Deployment = null;
        }
        else if (!string.IsNullOrWhiteSpace(DeployHost))
        {
            config.ServerProfileName = null;
            config.Deployment = new DeploymentConfig
            {
                Host = DeployHost,
                Port = DeployPort,
                Username = DeployUsername,
                EncryptedPassword = _configService is not null && !string.IsNullOrEmpty(DeployPassword)
                    ? _configService.EncryptPassword(DeployPassword)
                    : string.Empty,
                WorkingDirectory = DeployWorkingDirectory,
                UseSudo = DeployUseSudo
            };
        }

        return config;
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(RootDirectory);
}
