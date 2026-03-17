using System.Collections.ObjectModel;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;
using DockerManager.Core.Services;

namespace DockerManager.App.ViewModels;

public class ServerProfileEditDialogViewModel : ViewModelBase
{
    private readonly IConfigurationService? _configService;
    private readonly IDeploymentService? _deploymentService;

    private string _name = string.Empty;
    private string _host = string.Empty;
    private int _port = 22;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _workingDirectory = string.Empty;
    private bool _useSudo;
    private string _registryUrl = string.Empty;
    private string _registryUsername = string.Empty;
    private string _registryPassword = string.Empty;
    private string _newVariableKey = string.Empty;
    private string _newVariableValue = string.Empty;
    private string _testConnectionStatus = string.Empty;
    private bool _isTestingConnection;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool IsNew { get; set; } = true;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
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

    public string WorkingDirectory
    {
        get => _workingDirectory;
        set => SetProperty(ref _workingDirectory, value);
    }

    public bool UseSudo
    {
        get => _useSudo;
        set => SetProperty(ref _useSudo, value);
    }

    public string RegistryUrl
    {
        get => _registryUrl;
        set => SetProperty(ref _registryUrl, value);
    }

    public string RegistryUsername
    {
        get => _registryUsername;
        set => SetProperty(ref _registryUsername, value);
    }

    public string RegistryPassword
    {
        get => _registryPassword;
        set => SetProperty(ref _registryPassword, value);
    }

    public string NewVariableKey
    {
        get => _newVariableKey;
        set => SetProperty(ref _newVariableKey, value);
    }

    public string NewVariableValue
    {
        get => _newVariableValue;
        set => SetProperty(ref _newVariableValue, value);
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

    public ObservableCollection<CustomVariable> CustomVariables { get; } = new();

    public RelayCommand AddVariableCommand { get; }
    public RelayCommand RemoveVariableCommand { get; }
    public AsyncRelayCommand TestConnectionCommand { get; }

    public ServerProfileEditDialogViewModel()
        : this(null, null)
    {
    }

    public ServerProfileEditDialogViewModel(
        IConfigurationService? configService,
        IDeploymentService? deploymentService)
    {
        _configService = configService;
        _deploymentService = deploymentService;

        AddVariableCommand = new RelayCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(NewVariableKey)) return;
            CustomVariables.Add(new CustomVariable { Key = NewVariableKey.Trim(), Value = NewVariableValue });
            NewVariableKey = string.Empty;
            NewVariableValue = string.Empty;
        });

        RemoveVariableCommand = new RelayCommand(param =>
        {
            if (param is CustomVariable variable)
                CustomVariables.Remove(variable);
        });

        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync,
            () => !IsTestingConnection && !string.IsNullOrWhiteSpace(Host));
    }

    private async Task TestConnectionAsync()
    {
        if (_deploymentService is null || string.IsNullOrWhiteSpace(Host))
            return;

        IsTestingConnection = true;
        TestConnectionStatus = "Connessione in corso...";
        TestConnectionCommand.RaiseCanExecuteChanged();

        try
        {
            var config = new DeploymentConfig
            {
                Host = Host,
                Port = Port,
                Username = Username,
                WorkingDirectory = WorkingDirectory
            };

            var success = await _deploymentService.TestConnectionAsync(config, Password);
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

    public void LoadFrom(ServerProfile profile)
    {
        Id = profile.Id;
        IsNew = false;
        Name = profile.Name;
        Host = profile.Host;
        Port = profile.Port;
        Username = profile.Username;
        WorkingDirectory = profile.WorkingDirectory;
        UseSudo = profile.UseSudo;
        RegistryUrl = profile.RegistryUrl;
        RegistryUsername = profile.RegistryUsername;

        Password = _configService is not null && !string.IsNullOrEmpty(profile.EncryptedPassword)
            ? _configService.DecryptPassword(profile.EncryptedPassword)
            : string.Empty;

        RegistryPassword = _configService is not null && !string.IsNullOrEmpty(profile.EncryptedRegistryPassword)
            ? _configService.DecryptPassword(profile.EncryptedRegistryPassword)
            : string.Empty;

        CustomVariables.Clear();
        foreach (var (key, value) in profile.CustomVariables)
            CustomVariables.Add(new CustomVariable { Key = key, Value = value });
    }

    public ServerProfile ToServerProfile()
    {
        return new ServerProfile
        {
            Id = Id,
            Name = Name.Trim(),
            Host = Host.Trim(),
            Port = Port,
            Username = Username.Trim(),
            EncryptedPassword = _configService is not null && !string.IsNullOrEmpty(Password)
                ? _configService.EncryptPassword(Password)
                : string.Empty,
            WorkingDirectory = WorkingDirectory.Trim(),
            UseSudo = UseSudo,
            RegistryUrl = RegistryUrl.Trim(),
            RegistryUsername = RegistryUsername.Trim(),
            EncryptedRegistryPassword = _configService is not null && !string.IsNullOrEmpty(RegistryPassword)
                ? _configService.EncryptPassword(RegistryPassword)
                : string.Empty,
            CustomVariables = CustomVariables.ToDictionary(v => v.Key, v => v.Value)
        };
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Host);
}

public class CustomVariable : ViewModelBase
{
    private string _key = string.Empty;
    private string _value = string.Empty;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
