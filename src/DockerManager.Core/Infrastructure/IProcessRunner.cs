namespace DockerManager.Core.Infrastructure;

public interface IProcessRunner
{
    Task<int> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        string? stdinData = null,
        CancellationToken cancellationToken = default);
}
