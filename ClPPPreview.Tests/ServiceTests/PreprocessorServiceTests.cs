using ClPPPreview.Services;
using ClPPPreview.Models;

namespace ClPPPreview.Tests.ServiceTests;

[TestFixture]
public class PreprocessorServiceTests
{
    private PreprocessorService _preprocessorService;

    [SetUp]
    public void Setup()
    {
        _preprocessorService = new PreprocessorService();
    }

    [TearDown]
    public void TearDown()
    {
        _preprocessorService?.Dispose();
    }

    [Test]
    public void ValidateBuildToolPath_ShouldReturnFalse_ForNullOrEmptyPath()
    {
        // Act & Assert
        Assert.That(_preprocessorService.ValidateBuildToolPath(null), Is.False);
        Assert.That(_preprocessorService.ValidateBuildToolPath(""), Is.False);
        Assert.That(_preprocessorService.ValidateBuildToolPath("   "), Is.False);
    }

    [Test]
    public void ValidateBuildToolPath_ShouldReturnFalse_ForNonExistentFile()
    {
        // Act
        var result = _preprocessorService.ValidateBuildToolPath(@"C:\NonExistent\cl.exe");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateBuildToolPath_ShouldReturnFalse_ForNonClExe()
    {
        // Act
        var result = _preprocessorService.ValidateBuildToolPath(@"C:\Windows\System32\notepad.exe");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetDefaultCommandLineArgs_ShouldReturnExpectedArgs()
    {
        // Act
        var args = _preprocessorService.GetDefaultCommandLineArgs();

        // Assert
        Assert.That(args, Is.Not.Null);
        Assert.That(args.Length, Is.EqualTo(2));
        Assert.That(args, Contains.Item("/EP"));
        Assert.That(args, Contains.Item("/C"));
    }

    [Test]
    public void GetRecommendedArguments_ShouldReturnArgumentDictionary()
    {
        // Act
        var args = _preprocessorService.GetRecommendedArguments();

        // Assert
        Assert.That(args, Is.Not.Null);
        Assert.That(args.Count, Is.GreaterThan(0));
        Assert.That(args.ContainsKey("/EP"), Is.True);
        Assert.That(args.ContainsKey("/C"), Is.True);
        Assert.That(args["/EP"], Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void AutoDetectMSVCPath_ShouldReturnString()
    {
        // Act
        var result = _preprocessorService.AutoDetectMSVCPath();

        // Assert
        Assert.That(result, Is.Not.Null);
        // Note: Result might be null if MSVC is not installed, which is acceptable
    }

    [Test]
    public void ProcessSourceAsync_ShouldThrow_WithNullSourceCode()
    {
        // Arrange
        var config = new PreprocessorConfig 
        { 
            BuildToolPath = @"C:\fake\cl.exe",
            CommandLineArgs = "/EP"
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _preprocessorService.ProcessSourceAsync(null!, config));
    }

    [Test]
    public void ProcessSourceAsync_ShouldThrow_WithNullConfig()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _preprocessorService.ProcessSourceAsync("int main(){}", null!));
    }

    [Test]
    public void ProcessSourceAsync_ShouldThrow_WithInvalidConfig()
    {
        // Arrange
        var invalidConfig = new PreprocessorConfig
        {
            BuildToolPath = "", // Invalid
            CommandLineArgs = "/EP"
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() =>
            _preprocessorService.ProcessSourceAsync("int main(){}", invalidConfig));
    }

    [Test]
    public async Task ProcessSourceAsync_ShouldReturnFailure_WithNonExistentToolPath()
    {
        // Arrange
        var config = new PreprocessorConfig
        {
            BuildToolPath = @"C:\NonExistent\cl.exe",
            CommandLineArgs = "/EP"
        };
        var sourceCode = "#include <iostream>\nint main() { return 0; }";

        // Act
        var result = await _preprocessorService.ProcessSourceAsync(sourceCode, config);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorOutput, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task TestBuildToolAsync_ShouldReturnFalse_WithInvalidPath()
    {
        // Act
        var result = await _preprocessorService.TestBuildToolAsync(@"C:\NonExistent\cl.exe");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetCompilerVersionAsync_ShouldReturnNull_WithInvalidPath()
    {
        // Act
        var result = await _preprocessorService.GetCompilerVersionAsync(@"C:\NonExistent\cl.exe");

        // Assert
        Assert.That(result, Is.Null);
    }
}