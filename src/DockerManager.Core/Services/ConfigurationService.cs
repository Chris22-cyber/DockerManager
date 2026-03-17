using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DockerManager.Core.Models;

namespace DockerManager.Core.Services;

[SupportedOSPlatform("windows")]
public class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _configPath;

    public AppConfiguration Configuration { get; private set; } = new();

    public ConfigurationService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "DockerManager");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "config.json");
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            Configuration = new AppConfiguration();
            return;
        }

        var json = await File.ReadAllTextAsync(_configPath);
        Configuration = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions) ?? new AppConfiguration();
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(Configuration, JsonOptions);
        await File.WriteAllTextAsync(_configPath, json);
    }

    public void AddProject(ProjectConfig project)
    {
        Configuration.Projects.Add(project);
    }

    public void UpdateProject(ProjectConfig project)
    {
        var index = Configuration.Projects.FindIndex(p => p.Id == project.Id);
        if (index >= 0)
            Configuration.Projects[index] = project;
    }

    public void RemoveProject(string projectId)
    {
        Configuration.Projects.RemoveAll(p => p.Id == projectId);
    }

    public string EncryptPassword(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public string DecryptPassword(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return string.Empty;

        try
        {
            var bytes = Convert.FromBase64String(encrypted);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}
