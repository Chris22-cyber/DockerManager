namespace DockerManager.Core.Models;

public class RegistryCredentials
{
    public string Server { get; set; } = "https://index.docker.io/v1/";
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
}
