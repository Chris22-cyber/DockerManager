using DockerManager.Core.Services;
using Xunit;

namespace DockerManager.Core.Tests;

public class TagGeneratorServiceTests
{
    private readonly TagGeneratorService _service = new();

    [Fact]
    public void GenerateTimestampTag_ReturnsCorrectFormat()
    {
        var tag = _service.GenerateTimestampTag();
        Assert.Matches(@"^\d{8}-\d{6}$", tag);
    }

    [Theory]
    [InlineData(1, 0, 0, "v1.0.0")]
    [InlineData(2, 3, 1, "v2.3.1")]
    public void GenerateSemanticTag_ReturnsCorrectFormat(int major, int minor, int patch, string expected)
    {
        Assert.Equal(expected, _service.GenerateSemanticTag(major, minor, patch));
    }

    [Theory]
    [InlineData("docker.io/user", "myapp", "latest", "docker.io/user/myapp:latest")]
    [InlineData("", "myapp", "v1.0", "myapp:v1.0")]
    public void GetFullImageName_ReturnsCorrectFormat(string registry, string image, string tag, string expected)
    {
        Assert.Equal(expected, _service.GetFullImageName(registry, image, tag));
    }
}
