namespace DockerManager.Core.Services;

public interface ITagGeneratorService
{
    string GenerateTimestampTag();
    string GenerateSemanticTag(int major, int minor, int patch);
    string GetFullImageName(string registry, string imageName, string tag);
}
