namespace DockerManager.Core.Services;

public interface IDockerfileDiscoveryService
{
    IEnumerable<string> FindDockerfiles(string rootDirectory);
}
