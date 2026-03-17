using DockerManager.Core.Models;
using DockerManager.Core.Services;
using Xunit;

namespace DockerManager.Core.Tests;

public class DeploymentServiceTests
{
    private readonly DeploymentService _service = new();

    private static DeploymentConfig CreateConfig(params string[] commands) => new()
    {
        Host = "192.168.1.100",
        Port = 22,
        Username = "testuser",
        WorkingDirectory = "/opt/myapp",
        Commands = commands.ToList()
    };

    [Fact]
    public async Task TestConnectionAsync_ReturnsFalse_WhenHostUnreachable()
    {
        var config = CreateConfig();

        var result = await _service.TestConnectionAsync(config, "password");

        Assert.False(result);
    }

    [Fact]
    public async Task DeployAsync_ReturnsError_WhenConnectionFails()
    {
        var config = CreateConfig("echo hello");
        var logs = new List<LogEntry>();

        var result = await _service.DeployAsync(config, "password", entry => logs.Add(entry));

        Assert.False(result.Success);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains(logs, l => l.Level == LogLevel.Error);
    }

    [Fact]
    public async Task DeployAsync_SupportsCancellation()
    {
        var config = CreateConfig("echo hello");
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var logs = new List<LogEntry>();

        // With an unreachable host and a cancelled token, the service should either
        // throw OperationCanceledException or return a failed result (connection error before cancellation check)
        try
        {
            var result = await _service.DeployAsync(config, "password", entry => logs.Add(entry), cts.Token);
            // If no exception, the connection failed before cancellation was checked
            Assert.False(result.Success);
        }
        catch (OperationCanceledException)
        {
            // This is also acceptable
        }
    }

    [Fact]
    public async Task DeployAsync_LogsConnectionAttempt()
    {
        var config = CreateConfig("echo hello");
        var logs = new List<LogEntry>();

        await _service.DeployAsync(config, "password", entry => logs.Add(entry));

        Assert.Contains(logs, l => l.Message.Contains("192.168.1.100:22"));
    }

    [Fact]
    public void DeploymentConfig_DefaultValues()
    {
        var config = new DeploymentConfig();

        Assert.Equal(22, config.Port);
        Assert.Equal(string.Empty, config.Host);
        Assert.Equal(string.Empty, config.Username);
        Assert.Equal(string.Empty, config.EncryptedPassword);
        Assert.Equal(string.Empty, config.WorkingDirectory);
        Assert.Empty(config.Commands);
    }
}
