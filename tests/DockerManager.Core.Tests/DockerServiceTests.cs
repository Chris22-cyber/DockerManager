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
        var project = new ProjectConfig
        {
            Name = "test",
            DockerfilePath = @"C:\test\Dockerfile",
            ContextDirectory = @"C:\test",
            ImageName = "myapp",
            Registry = "docker.io/user"
        };

        _processRunner
            .Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Action<string>?>(), It.IsAny<Action<string>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.BuildAsync(project, "latest");

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
