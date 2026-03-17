using System.Diagnostics;
using System.Text;
using DockerManager.Core.Models;
using Renci.SshNet;

namespace DockerManager.Core.Services;

public class DeploymentService : IDeploymentService
{
    public async Task<DockerOperationResult> DeployAsync(
        DeploymentConfig config,
        string decryptedPassword,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var outputBuilder = new StringBuilder();

        onLog?.Invoke(new LogEntry
        {
            Level = LogLevel.Info,
            Message = $"Connessione a {config.Host}:{config.Port}..."
        });

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var client = CreateClient(config, decryptedPassword);
            client.Connect();

            cancellationToken.ThrowIfCancellationRequested();

            onLog?.Invoke(new LogEntry
            {
                Level = LogLevel.Success,
                Message = "Connessione SSH stabilita."
            });

            foreach (var command in config.Commands)
            {
                cancellationToken.ThrowIfCancellationRequested();

                onLog?.Invoke(new LogEntry
                {
                    Level = LogLevel.Info,
                    Message = $"Esecuzione: {command}"
                });

                var fullCommand = string.IsNullOrWhiteSpace(config.WorkingDirectory)
                    ? command
                    : $"cd {config.WorkingDirectory} && {command}";

                using var cmd = client.CreateCommand(fullCommand);
                var result = await Task.Run(() => cmd.Execute(), cancellationToken);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    outputBuilder.AppendLine(result);
                    foreach (var line in result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        onLog?.Invoke(new LogEntry
                        {
                            Level = LogLevel.Info,
                            Message = line.TrimEnd()
                        });
                    }
                }

                var error = cmd.Error;
                if (!string.IsNullOrWhiteSpace(error))
                {
                    foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        onLog?.Invoke(new LogEntry
                        {
                            Level = LogLevel.Error,
                            Message = line.TrimEnd()
                        });
                    }
                }

                if ((cmd.ExitStatus ?? -1) != 0)
                {
                    stopwatch.Stop();
                    onLog?.Invoke(new LogEntry
                    {
                        Level = LogLevel.Error,
                        Message = $"Comando fallito con exit code {(cmd.ExitStatus ?? -1)}: {command}"
                    });

                    return new DockerOperationResult
                    {
                        Success = false,
                        ExitCode = (cmd.ExitStatus ?? -1),
                        Output = outputBuilder.ToString(),
                        Error = error ?? string.Empty,
                        Duration = stopwatch.Elapsed
                    };
                }

                onLog?.Invoke(new LogEntry
                {
                    Level = LogLevel.Success,
                    Message = $"Completato: {command}"
                });
            }

            client.Disconnect();
            stopwatch.Stop();

            onLog?.Invoke(new LogEntry
            {
                Level = LogLevel.Success,
                Message = $"Deploy completato con successo in {stopwatch.Elapsed.TotalSeconds:F1}s"
            });

            return new DockerOperationResult
            {
                Success = true,
                ExitCode = 0,
                Output = outputBuilder.ToString(),
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            onLog?.Invoke(new LogEntry
            {
                Level = LogLevel.Warning,
                Message = "Deploy annullato."
            });
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            onLog?.Invoke(new LogEntry
            {
                Level = LogLevel.Error,
                Message = $"Errore SSH: {ex.Message}"
            });

            return new DockerOperationResult
            {
                Success = false,
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                Error = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<bool> TestConnectionAsync(
        DeploymentConfig config,
        string decryptedPassword)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var client = CreateClient(config, decryptedPassword);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
                client.Connect();
                var isConnected = client.IsConnected;
                client.Disconnect();
                return isConnected;
            });
        }
        catch
        {
            return false;
        }
    }

    private static SshClient CreateClient(DeploymentConfig config, string decryptedPassword)
    {
        return new SshClient(config.Host, config.Port, config.Username, decryptedPassword);
    }
}
