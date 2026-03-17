namespace DockerManager.Core.Models;

public class DockerImageConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DockerfilePath { get; set; } = string.Empty;
    public string ContextDirectory { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string Registry { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new() { "latest" };
    public Dictionary<string, string> BuildArgs { get; set; } = new();
}
