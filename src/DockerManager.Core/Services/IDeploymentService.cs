using DockerManager.Core.Models;

namespace DockerManager.Core.Services;

public interface IDeploymentService
{
    Task<DockerOperationResult> DeployAsync(
        DeploymentConfig config,
        string decryptedPassword,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(
        DeploymentConfig config,
        string decryptedPassword);
}
