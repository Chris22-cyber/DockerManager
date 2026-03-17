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

        if (MigrateEmbeddedDeploymentConfigs())
            await SaveAsync();
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

    public async Task ExportProjectAsync(string projectId, string filePath)
    {
        var project = Configuration.Projects.Find(p => p.Id == projectId);
        if (project is null)
            throw new InvalidOperationException($"Project with id '{projectId}' not found.");

        var json = JsonSerializer.Serialize(project, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ImportProjectAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<ProjectConfig>(json, JsonOptions);
        if (project is null)
            throw new InvalidOperationException("Invalid project file.");

        // Assign new ID to avoid conflicts
        project.Id = Guid.NewGuid().ToString();
        foreach (var image in project.Images)
            image.Id = Guid.NewGuid().ToString();

        Configuration.Projects.Add(project);
    }

    public async Task ExportAllProjectsAsync(string filePath)
    {
        var json = JsonSerializer.Serialize(Configuration.Projects, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    // --- Server Profiles ---

    public List<ServerProfile> GetAllProfiles() => Configuration.ServerProfiles;

    public ServerProfile? GetProfileByName(string name)
    {
        return Configuration.ServerProfiles
            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void AddProfile(ServerProfile profile)
    {
        if (Configuration.ServerProfiles.Any(p =>
                string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Un profilo con il nome '{profile.Name}' esiste già.");

        Configuration.ServerProfiles.Add(profile);
    }

    public void UpdateProfile(ServerProfile profile)
    {
        var index = Configuration.ServerProfiles.FindIndex(p => p.Id == profile.Id);
        if (index >= 0)
            Configuration.ServerProfiles[index] = profile;
    }

    public void RemoveProfile(string profileId)
    {
        var profile = Configuration.ServerProfiles.Find(p => p.Id == profileId);
        if (profile is null) return;

        // Clear references from projects
        foreach (var project in Configuration.Projects)
        {
            if (string.Equals(project.ServerProfileName, profile.Name, StringComparison.OrdinalIgnoreCase))
                project.ServerProfileName = null;
        }

        Configuration.ServerProfiles.RemoveAll(p => p.Id == profileId);
    }

    public ServerProfile? ResolveProfile(ProjectConfig project)
    {
        if (!string.IsNullOrEmpty(project.ServerProfileName))
            return GetProfileByName(project.ServerProfileName);
        return null;
    }

    private bool MigrateEmbeddedDeploymentConfigs()
    {
        var migrated = false;

        foreach (var project in Configuration.Projects)
        {
            if (project.Deployment is not { } deployment) continue;
            if (!string.IsNullOrEmpty(project.ServerProfileName)) continue;
            if (string.IsNullOrWhiteSpace(deployment.Host)) continue;

            // Check if a matching profile already exists
            var existing = Configuration.ServerProfiles.FirstOrDefault(p =>
                string.Equals(p.Host, deployment.Host, StringComparison.OrdinalIgnoreCase) &&
                p.Port == deployment.Port &&
                string.Equals(p.Username, deployment.Username, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                project.ServerProfileName = existing.Name;
            }
            else
            {
                var profileName = $"{project.Name} - {deployment.Host}";
                // Ensure unique name
                var baseName = profileName;
                var counter = 2;
                while (Configuration.ServerProfiles.Any(p =>
                           string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase)))
                {
                    profileName = $"{baseName} ({counter++})";
                }

                var profile = new ServerProfile
                {
                    Host = deployment.Host,
                    Port = deployment.Port,
                    Username = deployment.Username,
                    EncryptedPassword = deployment.EncryptedPassword,
                    WorkingDirectory = deployment.WorkingDirectory,
                    UseSudo = deployment.UseSudo,
                    Name = profileName
                };

                Configuration.ServerProfiles.Add(profile);
                project.ServerProfileName = profile.Name;
            }

            migrated = true;
        }

        return migrated;
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
