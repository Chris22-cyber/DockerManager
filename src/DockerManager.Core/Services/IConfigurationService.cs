using DockerManager.Core.Models;

namespace DockerManager.Core.Services;

public interface IConfigurationService
{
    AppConfiguration Configuration { get; }
    Task LoadAsync();
    Task SaveAsync();
    void AddProject(ProjectConfig project);
    void UpdateProject(ProjectConfig project);
    void RemoveProject(string projectId);
    string EncryptPassword(string plainText);
    string DecryptPassword(string encrypted);
    Task ExportProjectAsync(string projectId, string filePath);
    Task ImportProjectAsync(string filePath);
    Task ExportAllProjectsAsync(string filePath);
}
