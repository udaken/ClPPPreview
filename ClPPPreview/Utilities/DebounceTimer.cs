namespace ClPPPreview.Utilities;

/// <summary>
/// Timer that debounces rapid successive calls, executing the action only after the specified delay
/// </summary>
public class DebounceTimer : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly Action _action;
    private readonly int _delayMs;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the DebounceTimer
    /// </summary>
    /// <param name="action">Action to execute after the debounce delay</param>
    /// <param name="delayMs">Delay in milliseconds before executing the action</param>
    public DebounceTimer(Action action, int delayMs)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _delayMs = delayMs > 0 ? delayMs : throw new ArgumentException("Delay must be positive", nameof(delayMs));
        
        _timer = new System.Threading.Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Triggers the debounce timer. If called repeatedly within the delay period,
    /// only the last call will result in action execution.
    /// </summary>
    public void Trigger()
    {
        if (_disposed)
            return;

        // Reset the timer - this cancels any pending execution and starts a new delay
        _timer.Change(_delayMs, Timeout.Infinite);
    }

    /// <summary>
    /// Cancels any pending action execution
    /// </summary>
    public void Cancel()
    {
        if (_disposed)
            return;

        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Immediately executes the action and cancels any pending execution
    /// </summary>
    public void ExecuteImmediately()
    {
        if (_disposed)
            return;

        Cancel();
        _action();
    }

    /// <summary>
    /// Timer callback that executes the action
    /// </summary>
    /// <param name="state">Timer state (unused)</param>
    private void TimerCallback(object? state)
    {
        if (_disposed)
            return;

        try
        {
            _action();
        }
        catch (Exception ex)
        {
            // Log the exception but don't let it crash the timer
            System.Diagnostics.Debug.WriteLine($"Exception in DebounceTimer action: {ex}");
        }
    }

    /// <summary>
    /// Disposes of the timer
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _timer.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}