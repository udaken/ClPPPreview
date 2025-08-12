using ClPPPreview.Services;
using ClPPPreview.Models;

namespace ClPPPreview.Tests.ServiceTests;

[TestFixture]
public class ConfigManagerTests
{
    private ConfigManager _configManager;
    private string _testConfigPath;

    [SetUp]
    public void Setup()
    {
        // Use a temporary directory for testing
        _testConfigPath = Path.Combine(Path.GetTempPath(), "ClppPreviewTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testConfigPath);
        
        // We'll need to use reflection to set the config path for testing
        // For now, create a basic instance
        _configManager = new ConfigManager();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testConfigPath))
        {
            Directory.Delete(_testConfigPath, true);
        }
    }

    [Test]
    public void LoadConfig_ShouldReturnDefaultConfig_WhenNoConfigFileExists()
    {
        // Act
        var config = _configManager.LoadConfig();

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.CommandLineArgs, Is.EqualTo("/EP /C"));
        Assert.That(config.DebounceDelayMs, Is.EqualTo(500));
        Assert.That(config.WindowWidth, Is.EqualTo(913));
        Assert.That(config.WindowHeight, Is.EqualTo(936));
    }

    [Test]
    public void ValidateExecutablePath_ShouldReturnFalse_ForNullOrEmptyPath()
    {
        // Act & Assert
        Assert.That(_configManager.ValidateExecutablePath(null), Is.False);
        Assert.That(_configManager.ValidateExecutablePath(""), Is.False);
        Assert.That(_configManager.ValidateExecutablePath("   "), Is.False);
    }

    [Test]
    public void ValidateExecutablePath_ShouldReturnFalse_ForNonExistentFile()
    {
        // Act
        var result = _configManager.ValidateExecutablePath(@"C:\NonExistent\cl.exe");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void FindMSVCToolPath_ShouldReturnString()
    {
        // Act
        var result = _configManager.FindMSVCToolPath();

        // Assert
        Assert.That(result, Is.Not.Null);
        // Note: Result might be empty if MSVC is not installed, which is fine
    }
}