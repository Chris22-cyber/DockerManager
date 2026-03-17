namespace DockerManager.Core.Models;

public class ServerProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    // SSH
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool UseSudo { get; set; }

    // Registry (optional override)
    public string RegistryUrl { get; set; } = string.Empty;
    public string RegistryUsername { get; set; } = string.Empty;
    public string EncryptedRegistryPassword { get; set; } = string.Empty;

    // Custom variables for ${VAR_NAME} substitution in deploy commands
    public Dictionary<string, string> CustomVariables { get; set; } = new();
}
