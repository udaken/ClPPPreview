using ClPPPreview.Services;

namespace ClPPPreview.Tests.ServiceTests;

[TestFixture]
public class FileManagerTests
{
    private FileManager _fileManager;

    [SetUp]
    public void Setup()
    {
        _fileManager = new FileManager();
    }

    [TearDown]
    public void TearDown()
    {
        _fileManager?.Dispose();
    }

    [Test]
    public void CreateTemporarySourceFile_ShouldCreateFile_WithValidContent()
    {
        // Arrange
        const string content = "#include <iostream>\nint main() { return 0; }";

        // Act
        var filePath = _fileManager.CreateTemporarySourceFile(content);

        // Assert
        Assert.That(File.Exists(filePath), Is.True);
        Assert.That(filePath, Does.EndWith(".cpp"));
        
        var fileContent = File.ReadAllText(filePath);
        Assert.That(fileContent, Is.EqualTo(content));
        
        // Cleanup
        _fileManager.DeleteTemporaryFile(filePath);
    }

    [Test]
    public void CreateTemporarySourceFile_ShouldThrow_WithNullContent()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _fileManager.CreateTemporarySourceFile(null));
    }

    [Test]
    public void CreateTemporarySourceFile_ShouldThrow_WithEmptyContent()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _fileManager.CreateTemporarySourceFile(""));
    }

    [Test]
    public void DeleteTemporaryFile_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        var filePath = _fileManager.CreateTemporarySourceFile("test content");
        
        // Act
        var result = _fileManager.DeleteTemporaryFile(filePath);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(File.Exists(filePath), Is.False);
    }

    [Test]
    public void DeleteTemporaryFile_ShouldReturnFalse_WithNullPath()
    {
        // Act
        var result = _fileManager.DeleteTemporaryFile(null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateExecutablePath_ShouldReturnFalse_ForInvalidPaths()
    {
        // Act & Assert
        Assert.That(_fileManager.ValidateExecutablePath(null), Is.False);
        Assert.That(_fileManager.ValidateExecutablePath(""), Is.False);
        Assert.That(_fileManager.ValidateExecutablePath("notanexe.txt"), Is.False);
        Assert.That(_fileManager.ValidateExecutablePath(@"C:\NonExistent\file.exe"), Is.False);
    }

    [Test]
    public void GetTemporaryDirectoryInfo_ShouldReturnValidInfo()
    {
        // Act
        var (path, sizeBytes, fileCount) = _fileManager.GetTemporaryDirectoryInfo();

        // Assert
        Assert.That(path, Is.Not.Null.And.Not.Empty);
        Assert.That(sizeBytes, Is.GreaterThanOrEqualTo(0));
        Assert.That(fileCount, Is.GreaterThanOrEqualTo(0));
    }
}