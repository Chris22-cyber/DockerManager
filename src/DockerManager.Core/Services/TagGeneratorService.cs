namespace DockerManager.Core.Services;

public class TagGeneratorService : ITagGeneratorService
{
    public string GenerateTimestampTag() =>
        DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    public string GenerateSemanticTag(int major, int minor, int patch) =>
        $"v{major}.{minor}.{patch}";

    public string GetFullImageName(string registry, string imageName, string tag)
    {
        var image = string.IsNullOrEmpty(registry)
            ? imageName
            : $"{registry}/{imageName}";
        return $"{image}:{tag}";
    }
}
