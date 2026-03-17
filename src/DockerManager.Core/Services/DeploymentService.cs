using System.Diagnostics;
using System.Text;
using DockerManager.Core.Models;
using Renci.SshNet;

namespace DockerManager.Core.Services;

public class DeploymentService : IDeploymentService
{
    public Task<DockerOperationResult> DeployAsync(
        DeploymentConfig config,
        string decryptedPassword,
        List<string> commands,
        Action<LogEntry>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        return DeployAsync(config, decryptedPassword, commands, null, onLog, cancellationToken);
    }

    public async Task<DockerOperationResult> DeployAsync(
        DeploymentConfig config,
        string decryptedPassword,
        List<string> commands,
        Dictionary<string, string>? variables,
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

            foreach (var rawCommand in commands)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var command = SubstituteVariables(rawCommand, variables);

                onLog?.Invoke(new LogEntry
                {
                    Level = LogLevel.Info,
                    Message = $"Esecuzione: {command}"
                });

                string fullCommand;
                var innerCommand = string.IsNullOrWhiteSpace(config.WorkingDirectory)
                    ? command
                    : $"cd {config.WorkingDirectory} && {command}";

                if (config.UseSudo)
                {
                    var escapedInner = innerCommand.Replace("'", "'\\''");
                    fullCommand = $"echo '{decryptedPassword.Replace("'", "'\\''")}' | sudo -S sh -c '{escapedInner}'";
                }
                else
                {
                    fullCommand = innerCommand;
                }

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

    private static string SubstituteVariables(string command, Dictionary<string, string>? variables)
    {
        if (variables is null || variables.Count == 0)
            return command;

        foreach (var (key, value) in variables)
            command = command.Replace($"${{{key}}}", value);

        return command;
    }

    private static SshClient CreateClient(DeploymentConfig config, string decryptedPassword)
    {
        return new SshClient(config.Host, config.Port, config.Username, decryptedPassword);
    }
}
