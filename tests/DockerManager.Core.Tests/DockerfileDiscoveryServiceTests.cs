using DockerManager.Core.Services;

namespace DockerManager.Core.Tests;

public class DockerfileDiscoveryServiceTests
{
    [Fact]
    public void FindDockerfiles_ReturnsEmpty_WhenDirectoryDoesNotExist()
    {
        var service = new DockerfileDiscoveryService();
        var results = service.FindDockerfiles(@"C:\nonexistent_path_12345");
        Assert.Empty(results);
    }
}
