namespace DockerManager.Core.Models;

public class ProjectConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string RootDirectory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DockerImageConfig> Images { get; set; } = new();
    public Dictionary<string, string> BuildArgs { get; set; } = new();
}
