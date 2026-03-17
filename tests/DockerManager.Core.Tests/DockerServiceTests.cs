using DockerManager.Core.Infrastructure;
using Xunit;
using DockerManager.Core.Models;
using DockerManager.Core.Services;
using Moq;

namespace DockerManager.Core.Tests;

public class DockerServiceTests
{
    private readonly Mock<IProcessRunner> _processRunner = new();
    private readonly Mock<IConfigurationService> _configService = new();
    private readonly DockerService _service;

    public DockerServiceTests()
    {
        _configService.Setup(c => c.Configuration).Returns(new AppConfiguration());
        _service = new DockerService(_processRunner.Object, _configService.Object);
    }

    [Fact]
    public async Task IsDockerAvailableAsync_ReturnsTrue_WhenExitCodeZero()
    {
        _processRunner
            .Setup(p => p.RunAsync("docker", "version", null, null, null, null, default))
            .ReturnsAsync(0);

        var result = await _service.IsDockerAvailableAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task IsDockerAvailableAsync_ReturnsFalse_WhenExceptionThrown()
    {
        _processRunner
            .Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Action<string>?>(), It.IsAny<Action<string>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("not found"));

        var result = await _service.IsDockerAvailableAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task BuildAsync_CallsProcessRunner_WithCorrectArgs()
    {
        var image = new DockerImageConfig
        {
            Name = "api",
            DockerfilePath = "Dockerfile",
            ContextDirectory = ".",
            ImageName = "myapp",
            Registry = "docker.io/user"
        };

        _processRunner
            .Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Action<string>?>(), It.IsAny<Action<string>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.BuildAsync(image, @"C:\test", "latest");

        Assert.True(result.Success);
        _processRunner.Verify(p => p.RunAsync(
            "docker",
            It.Is<string>(s => s.Contains("build") && s.Contains("docker.io/user/myapp:latest")),
            It.IsAny<string?>(),
            It.IsAny<Action<string>?>(),
            It.IsAny<Action<string>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildAsync_MergesGlobalAndImageBuildArgs()
    {
        var image = new DockerImageConfig
        {
            Name = "api",
            DockerfilePath = "Dockerfile",
            ContextDirectory = ".",
            ImageName = "myapp",
            BuildArgs = new Dictionary<string, string> { { "ENV", "prod" } }
        };

        var globalArgs = new Dictionary<string, string>
        {
            { "DOTNET_VERSION", "8.0" },
            { "ENV", "staging" } // should be overridden by image-specific
        };

        _processRunner
            .Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Action<string>?>(), It.IsAny<Action<string>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.BuildAsync(image, @"C:\test", "latest", globalArgs);

        Assert.True(result.Success);
        _processRunner.Verify(p => p.RunAsync(
            "docker",
            It.Is<string>(s =>
                s.Contains("--build-arg DOTNET_VERSION=8.0") &&
                s.Contains("--build-arg ENV=prod") &&
                !s.Contains("ENV=staging")),
            It.IsAny<string?>(),
            It.IsAny<Action<string>?>(),
            It.IsAny<Action<string>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildAllAsync_BuildsAllImagesInProject()
    {
        var project = new ProjectConfig
        {
            Name = "test-project",
            RootDirectory = @"C:\test",
            Images = new List<DockerImageConfig>
            {
                new() { Name = "api", DockerfilePath = "api/Dockerfile", ContextDirectory = "api", ImageName = "myapp-api", Tags = new() { "latest" } },
                new() { Name = "web", DockerfilePath = "web/Dockerfile", ContextDirectory = "web", ImageName = "myapp-web", Tags = new() { "latest" } }
            }
        };

        _processRunner
            .Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Action<string>?>(), It.IsAny<Action<string>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var results = await _service.BuildAllAsync(project);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task LoginAsync_PassesPasswordViaStdin()
    {
        _processRunner
            .Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Action<string>?>(), It.IsAny<Action<string>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _service.LoginAsync("https://index.docker.io/v1/", "user", "secret");

        _processRunner.Verify(p => p.RunAsync(
            "docker",
            It.Is<string>(s => s.Contains("--password-stdin") && !s.Contains("secret")),
            It.IsAny<string?>(),
            It.IsAny<Action<string>?>(),
            It.IsAny<Action<string>?>(),
            "secret",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
