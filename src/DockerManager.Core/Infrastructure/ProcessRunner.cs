using System.Diagnostics;

namespace DockerManager.Core.Infrastructure;

public class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        string? stdinData = null,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdinData is not null,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var tcs = new TaskCompletionSource<int>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                onOutput?.Invoke(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                onError?.Invoke(e.Data);
        };

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            tcs.TrySetResult(process.ExitCode);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (stdinData is not null)
        {
            await process.StandardInput.WriteAsync(stdinData);
            process.StandardInput.Close();
        }

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                KillProcessTree(process);
            }
            catch { }
            tcs.TrySetCanceled(cancellationToken);
        });

        return await tcs.Task;
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            try { process.Kill(); } catch { }
        }
    }
}
