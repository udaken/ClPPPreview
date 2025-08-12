using ClPPPreview.Utilities;

namespace ClPPPreview.Tests.UtilityTests;

[TestFixture]
public class DebounceTimerTests
{
    [Test]
    public void Constructor_ShouldThrow_WithNullAction()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DebounceTimer(null, 500));
    }

    [Test]
    public void Constructor_ShouldThrow_WithZeroDelay()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DebounceTimer(() => { }, 0));
    }

    [Test]
    public void Constructor_ShouldThrow_WithNegativeDelay()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DebounceTimer(() => { }, -100));
    }

    [Test]
    public async Task Trigger_ShouldExecuteAction_AfterDelay()
    {
        // Arrange
        var executed = false;
        using var timer = new DebounceTimer(() => executed = true, 100);

        // Act
        timer.Trigger();
        await Task.Delay(200); // Wait longer than the debounce delay

        // Assert
        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task Trigger_ShouldDebounce_MultipleRapidCalls()
    {
        // Arrange
        var executionCount = 0;
        using var timer = new DebounceTimer(() => executionCount++, 100);

        // Act
        timer.Trigger();
        await Task.Delay(50);  // Less than debounce delay
        timer.Trigger();
        await Task.Delay(50);  // Less than debounce delay
        timer.Trigger();
        await Task.Delay(150); // Wait for final execution

        // Assert
        Assert.That(executionCount, Is.EqualTo(1)); // Should only execute once
    }

    [Test]
    public async Task ExecuteImmediately_ShouldExecuteAction_Immediately()
    {
        // Arrange
        var executed = false;
        using var timer = new DebounceTimer(() => executed = true, 1000);

        // Act
        timer.ExecuteImmediately();

        // Assert
        Assert.That(executed, Is.True); // Should execute immediately, not after 1000ms
    }

    [Test]
    public async Task Cancel_ShouldPreventExecution()
    {
        // Arrange
        var executed = false;
        using var timer = new DebounceTimer(() => executed = true, 100);

        // Act
        timer.Trigger();
        timer.Cancel();
        await Task.Delay(200);

        // Assert
        Assert.That(executed, Is.False);
    }
}