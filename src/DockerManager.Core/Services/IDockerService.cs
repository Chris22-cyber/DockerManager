using DockerManager.Core.Models;

namespace DockerManager.Core.Services;

public interface IDockerService
{
    Task<DockerOperationResult> BuildAsync(
        DockerImageConfig image,
        string rootDirectory,
        string tag,
        Dictionary<string, string>? globalBuildArgs = null,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<DockerOperationResult> PushAsync(
        string image,
        string tag,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<List<DockerOperationResult>> BuildAllAsync(
        ProjectConfig project,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<List<DockerOperationResult>> PushAllAsync(
        ProjectConfig project,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<DockerOperationResult> LoginAsync(
        string server,
        string username,
        string password,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsDockerAvailableAsync();
}
