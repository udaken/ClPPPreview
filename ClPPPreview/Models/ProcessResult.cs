namespace ClPPPreview.Models;

/// <summary>
/// Result of external process execution
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// Standard output from the process
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>
    /// Standard error output from the process
    /// </summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>
    /// Exit code returned by the process
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Whether the process execution was successful (exit code 0)
    /// </summary>
    public bool Success => ExitCode == 0;

    /// <summary>
    /// Duration of the process execution
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Whether the process was cancelled
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// Exception that occurred during process execution, if any
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets the combined output (stdout + stderr)
    /// </summary>
    public string CombinedOutput
    {
        get
        {
            var combined = StandardOutput;
            if (!string.IsNullOrEmpty(StandardError))
            {
                if (!string.IsNullOrEmpty(combined))
                    combined += Environment.NewLine;
                combined += StandardError;
            }
            return combined;
        }
    }
}