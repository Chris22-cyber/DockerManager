namespace DockerManager.Core.Models;

public class AppConfiguration
{
    public string DockerCliPath { get; set; } = "docker";
    public RegistryCredentials DefaultRegistry { get; set; } = new();
    public List<ProjectConfig> Projects { get; set; } = new();
}
