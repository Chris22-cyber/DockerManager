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
        DockerImageConfig image,
        string rootDirectory,
        string tag,
        Dictionary<string, string>? globalBuildArgs = null,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        var fullImage = string.IsNullOrEmpty(image.Registry)
            ? $"{image.ImageName}:{tag}"
            : $"{image.Registry}/{image.ImageName}:{tag}";

        var dockerfilePath = Path.IsPathRooted(image.DockerfilePath)
            ? image.DockerfilePath
            : Path.Combine(rootDirectory, image.DockerfilePath);

        var contextDir = Path.IsPathRooted(image.ContextDirectory)
            ? image.ContextDirectory
            : Path.Combine(rootDirectory, image.ContextDirectory);

        var args = new StringBuilder();
        args.Append($"build -t {fullImage}");
        args.Append($" -f \"{dockerfilePath}\"");

        // Global build args (inherited from project)
        if (globalBuildArgs != null)
        {
            foreach (var (key, value) in globalBuildArgs)
            {
                if (!image.BuildArgs.ContainsKey(key))
                    args.Append($" --build-arg {key}={value}");
            }
        }

        // Image-specific build args (override globals)
        foreach (var (key, value) in image.BuildArgs)
        {
            args.Append($" --build-arg {key}={value}");
        }

        args.Append($" \"{contextDir}\"");

        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Building {fullImage}..."
        });

        return await RunDockerCommandAsync(args.ToString(), contextDir, onLog, cancellationToken);
    }

    public async Task<List<DockerOperationResult>> BuildAllAsync(
        ProjectConfig project,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DockerOperationResult>();

        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Building all images for project '{project.Name}' ({project.Images.Count} images)..."
        });

        foreach (var image in project.Images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var tag in image.Tags)
            {
                var result = await BuildAsync(image, project.RootDirectory, tag, project.BuildArgs, onLog, cancellationToken);
                results.Add(result);

                if (!result.Success)
                {
                    onLog?.Invoke(new LogEntry
                    {
                        Level = LogLevel.Error,
                        Message = $"Build failed for image '{image.Name}' with tag '{tag}'. Stopping."
                    });
                    return results;
                }
            }
        }

        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Success,
            Message = $"All images built successfully for project '{project.Name}'."
        });

        return results;
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

    public async Task<List<DockerOperationResult>> PushAllAsync(
        ProjectConfig project,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DockerOperationResult>();

        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Pushing all images for project '{project.Name}' ({project.Images.Count} images)..."
        });

        foreach (var image in project.Images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullImageName = string.IsNullOrEmpty(image.Registry)
                ? image.ImageName
                : $"{image.Registry}/{image.ImageName}";

            foreach (var tag in image.Tags)
            {
                var result = await PushAsync(fullImageName, tag, onLog, cancellationToken);
                results.Add(result);

                if (!result.Success)
                {
                    onLog?.Invoke(new LogEntry
                    {
                        Level = LogLevel.Error,
                        Message = $"Push failed for image '{image.Name}' with tag '{tag}'. Stopping."
                    });
                    return results;
                }
            }
        }

        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Success,
            Message = $"All images pushed successfully for project '{project.Name}'."
        });

        return results;
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
