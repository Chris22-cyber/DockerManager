using System.Collections.ObjectModel;
using DockerManager.Core.Infrastructure;
using DockerManager.Core.Models;

namespace DockerManager.App.ViewModels;

public class LogOutputViewModel : ViewModelBase
{
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public RelayCommand ClearCommand { get; }

    public LogOutputViewModel()
    {
        ClearCommand = new RelayCommand(() => LogEntries.Clear());
    }

    public void AddLog(LogEntry entry)
    {
        LogEntries.Add(entry);
    }

    public void AddLog(string message, LogLevel level = LogLevel.Info)
    {
        LogEntries.Add(new LogEntry { Message = message, Level = level });
    }
}
