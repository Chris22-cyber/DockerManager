namespace DockerManager.Core.Models;

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Success
}

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string Message { get; set; } = string.Empty;

    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] {Message}";
}
