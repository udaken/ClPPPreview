using ClPPPreview.Services;

namespace ClPPPreview.Tests.ServiceTests;

[TestFixture]
public class ProcessExecutorTests
{
    private ProcessExecutor _processExecutor;

    [SetUp]
    public void Setup()
    {
        _processExecutor = new ProcessExecutor();
    }

    [TearDown]
    public void TearDown()
    {
        _processExecutor?.Dispose();
    }

    [Test]
    public void ExecuteAsync_ShouldThrow_WithNullExecutable()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _processExecutor.ExecuteAsync(null, "", ""));
    }

    [Test]
    public void ExecuteAsync_ShouldThrow_WithEmptyExecutable()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _processExecutor.ExecuteAsync("", "", ""));
    }

    [Test]
    public void ExecuteAsync_ShouldThrow_WithNonExistentExecutable()
    {
        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _processExecutor.ExecuteAsync(@"C:\NonExistent\fake.exe", "", ""));
    }

    [Test]
    public async Task ExecuteAsync_ShouldExecuteSuccessfully_WithValidCommand()
    {
        // Arrange - Use a command that should exist on Windows
        var executable = @"C:\Windows\System32\cmd.exe";
        var arguments = "/c echo Hello World";

        // Skip test if cmd.exe doesn't exist (unlikely but possible in some environments)
        if (!File.Exists(executable))
        {
            Assert.Ignore("cmd.exe not found - skipping test");
            return;
        }

        // Act
        var result = await _processExecutor.ExecuteAsync(executable, arguments, "");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StandardOutput, Does.Contain("Hello World"));
    }

    [Test]
    public async Task ExecuteAsync_ShouldHandleTimeout()
    {
        // Arrange - Use a command that will timeout
        var executable = @"C:\Windows\System32\cmd.exe";
        var arguments = "/c timeout /t 10 /nobreak > nul"; // Wait 10 seconds
        
        if (!File.Exists(executable))
        {
            Assert.Ignore("cmd.exe not found - skipping test");
            return;
        }

        // Act
        var result = await _processExecutor.ExecuteAsync(executable, arguments, "", default, 1000); // 1 second timeout

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.WasCancelled, Is.True);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task ExecuteAsync_ShouldHandleCancellation()
    {
        // Arrange
        var executable = @"C:\Windows\System32\cmd.exe";
        var arguments = "/c timeout /t 10 /nobreak > nul"; // Wait 10 seconds
        
        if (!File.Exists(executable))
        {
            Assert.Ignore("cmd.exe not found - skipping test");
            return;
        }

        using var cts = new CancellationTokenSource();

        // Act
        var task = _processExecutor.ExecuteAsync(executable, arguments, "", cts.Token, 30000);
        cts.CancelAfter(500); // Cancel after 500ms
        var result = await task;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.WasCancelled, Is.True);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task ExecuteSimpleAsync_ShouldReturnOutput()
    {
        // Arrange
        var executable = @"C:\Windows\System32\cmd.exe";
        var arguments = "/c echo Simple Test";

        if (!File.Exists(executable))
        {
            Assert.Ignore("cmd.exe not found - skipping test");
            return;
        }

        // Act
        var output = await _processExecutor.ExecuteSimpleAsync(executable, arguments);

        // Assert
        Assert.That(output, Does.Contain("Simple Test"));
    }

    [Test]
    public void KillAllProcesses_ShouldNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _processExecutor.KillAllProcesses());
    }
}