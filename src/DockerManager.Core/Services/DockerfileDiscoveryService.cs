namespace DockerManager.Core.Services;

public class DockerfileDiscoveryService : IDockerfileDiscoveryService
{
    private static readonly string[] DockerfilePatterns = { "Dockerfile", "Dockerfile.*", "*.dockerfile" };

    public IEnumerable<string> FindDockerfiles(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            yield break;

        foreach (var pattern in DockerfilePatterns)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(rootDirectory, pattern, SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }
}
