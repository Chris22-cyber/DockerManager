using DockerManager.Core.Models;

namespace DockerManager.Core.Services;

public interface IDockerService
{
    Task<DockerOperationResult> BuildAsync(
        ProjectConfig project,
        string tag,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<DockerOperationResult> TagAsync(
        string sourceImage,
        string targetImage,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<DockerOperationResult> PushAsync(
        string image,
        string tag,
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
