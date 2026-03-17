using System.Collections.ObjectModel;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IDockerService _dockerService;
    private string _dockerCliPath = "docker";
    private string _registryServer = "https://index.docker.io/v1/";
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isDockerAvailable;
    private string _statusMessage = string.Empty;
    private ServerProfile? _selectedProfile;

    public string DockerCliPath
    {
        get => _dockerCliPath;
        set => SetProperty(ref _dockerCliPath, value);
    }

    public string RegistryServer
    {
        get => _registryServer;
        set => SetProperty(ref _registryServer, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool IsDockerAvailable
    {
        get => _isDockerAvailable;
        set => SetProperty(ref _isDockerAvailable, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<ServerProfile> ServerProfiles { get; } = new();

    public ServerProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand TestDockerCommand { get; }
    public AsyncRelayCommand TestLoginCommand { get; }
    public RelayCommand AddProfileCommand { get; }
    public RelayCommand EditProfileCommand { get; }
    public RelayCommand RemoveProfileCommand { get; }

    public event Action? RequestAddProfile;
    public event Action<ServerProfile>? RequestEditProfile;

    public SettingsViewModel(IConfigurationService configService, IDockerService dockerService)
    {
        _configService = configService;
        _dockerService = dockerService;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        TestDockerCommand = new AsyncRelayCommand(TestDockerAsync);
        TestLoginCommand = new AsyncRelayCommand(TestLoginAsync);

        AddProfileCommand = new RelayCommand(() => RequestAddProfile?.Invoke());
        EditProfileCommand = new RelayCommand(param =>
        {
            if (param is ServerProfile profile)
                RequestEditProfile?.Invoke(profile);
        });
        RemoveProfileCommand = new RelayCommand(param =>
        {
            if (param is ServerProfile profile)
                RemoveProfile(profile);
        });
    }

    public void Load()
    {
        var config = _configService.Configuration;
        DockerCliPath = config.DockerCliPath;
        RegistryServer = config.DefaultRegistry.Server;
        Username = config.DefaultRegistry.Username;
        Password = string.IsNullOrEmpty(config.DefaultRegistry.EncryptedPassword)
            ? string.Empty
            : _configService.DecryptPassword(config.DefaultRegistry.EncryptedPassword);

        RefreshProfiles();
    }

    public void RefreshProfiles()
    {
        ServerProfiles.Clear();
        foreach (var profile in _configService.GetAllProfiles())
            ServerProfiles.Add(profile);
    }

    private async void RemoveProfile(ServerProfile profile)
    {
        _configService.RemoveProfile(profile.Id);
        await _configService.SaveAsync();
        RefreshProfiles();
    }

    private async Task SaveAsync()
    {
        var config = _configService.Configuration;
        config.DockerCliPath = DockerCliPath;
        config.DefaultRegistry.Server = RegistryServer;
        config.DefaultRegistry.Username = Username;

        if (!string.IsNullOrEmpty(Password))
            config.DefaultRegistry.EncryptedPassword = _configService.EncryptPassword(Password);

        await _configService.SaveAsync();
        StatusMessage = "Impostazioni salvate.";
    }

    private async Task TestDockerAsync()
    {
        IsDockerAvailable = await _dockerService.IsDockerAvailableAsync();
        StatusMessage = IsDockerAvailable
            ? "Docker disponibile e funzionante."
            : "Docker non trovato. Verifica il percorso e che Docker Desktop sia in esecuzione.";
    }

    private async Task TestLoginAsync()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            StatusMessage = "Inserisci username e password.";
            return;
        }

        var result = await _dockerService.LoginAsync(RegistryServer, Username, Password);
        StatusMessage = result.Success
            ? "Login riuscito!"
            : $"Login fallito: {result.Error}";
    }
}
