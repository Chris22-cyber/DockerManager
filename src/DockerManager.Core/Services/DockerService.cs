using System.Diagnostics;
using System.Text;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;

namespace DockerManager.Core.Services;

public class DockerService : IDockerService
{
    private readonly IProcessRunner _processRunner;
    private readonly IConfigurationService _configService;

    public DockerService(IProcessRunner processRunner, IConfigurationService configService)
    {
        _processRunner = processRunner;
        _configService = configService;
    }

    private string DockerPath => _configService.Configuration.DockerCliPath;

    public async Task<DockerOperationResult> BuildAsync(
        ProjectConfig project,
        string tag,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        var fullImage = string.IsNullOrEmpty(project.Registry)
            ? $"{project.ImageName}:{tag}"
            : $"{project.Registry}/{project.ImageName}:{tag}";

        var args = new StringBuilder();
        args.Append($"build -t {fullImage}");
        args.Append($" -f \"{project.DockerfilePath}\"");

        foreach (var (key, value) in project.BuildArgs)
        {
            args.Append($" --build-arg {key}={value}");
        }

        args.Append($" \"{project.ContextDirectory}\"");

        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Building {fullImage}..."
        });

        return await RunDockerCommandAsync(args.ToString(), project.ContextDirectory, onLog, cancellationToken);
    }

    public async Task<DockerOperationResult> TagAsync(
        string sourceImage,
        string targetImage,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Tagging {sourceImage} -> {targetImage}..."
        });

        return await RunDockerCommandAsync($"tag {sourceImage} {targetImage}", null, onLog, cancellationToken);
    }

    public async Task<DockerOperationResult> PushAsync(
        string image,
        string tag,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        var fullImage = $"{image}:{tag}";
        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Pushing {fullImage}..."
        });

        return await RunDockerCommandAsync($"push {fullImage}", null, onLog, cancellationToken);
    }

    public async Task<DockerOperationResult> LoginAsync(
        string server,
        string username,
        string password,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Logging in to {server} as {username}..."
        });

        var output = new StringBuilder();
        var error = new StringBuilder();
        var sw = Stopwatch.StartNew();

        var exitCode = await _processRunner.RunAsync(
            DockerPath,
            $"login {server} -u {username} --password-stdin",
            null,
            line =>
            {
                output.AppendLine(line);
                onLog?.Invoke(new LogEntry { Message = line });
            },
            line =>
            {
                error.AppendLine(line);
                onLog?.Invoke(new LogEntry { Level = LogLevel.Error, Message = line });
            },
            stdinData: password,
            cancellationToken: cancellationToken);

        sw.Stop();

        var success = exitCode == 0;
        onLog?.Invoke(new LogEntry
        {
            Level = success ? LogLevel.Success : LogLevel.Error,
            Message = success ? "Login succeeded." : $"Login failed (exit code {exitCode})."
        });

        return new DockerOperationResult
        {
            Success = success,
            ExitCode = exitCode,
            Output = output.ToString(),
            Error = error.ToString(),
            Duration = sw.Elapsed
        };
    }

    public async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            var exitCode = await _processRunner.RunAsync(DockerPath, "version");
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<DockerOperationResult> RunDockerCommandAsync(
        string arguments,
        string? workingDirectory,
        Action<LogEntry>? onLog,
        CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();
        var sw = Stopwatch.StartNew();

        var exitCode = await _processRunner.RunAsync(
            DockerPath,
            arguments,
            workingDirectory,
            line =>
            {
                output.AppendLine(line);
                onLog?.Invoke(new LogEntry { Message = line });
            },
            line =>
            {
                error.AppendLine(line);
                onLog?.Invoke(new LogEntry { Level = LogLevel.Error, Message = line });
            },
            cancellationToken: cancellationToken);

        sw.Stop();

        var success = exitCode == 0;
        onLog?.Invoke(new LogEntry
        {
            Level = success ? LogLevel.Success : LogLevel.Error,
            Message = success
                ? $"Completed successfully in {sw.Elapsed.TotalSeconds:F1}s."
                : $"Failed with exit code {exitCode}."
        });

        return new DockerOperationResult
        {
            Success = success,
            ExitCode = exitCode,
            Output = output.ToString(),
            Error = error.ToString(),
            Duration = sw.Elapsed
        };
    }
}
