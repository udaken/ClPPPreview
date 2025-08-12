namespace ClPPPreview.Models;

/// <summary>
/// Result of C++ preprocessing operation
/// </summary>
public class PreprocessResult
{
    /// <summary>
    /// Whether the preprocessing operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The preprocessed output from cl.exe
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Error output from cl.exe (stderr)
    /// </summary>
    public string ErrorOutput { get; set; } = string.Empty;

    /// <summary>
    /// Exit code from cl.exe process
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Duration of the preprocessing operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Timestamp when preprocessing started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets a user-friendly error message
    /// </summary>
    public string GetErrorMessage()
    {
        if (Success)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(ErrorOutput))
            return ErrorOutput.Trim();

        if (ExitCode != 0)
            return $"Process exited with code {ExitCode}";

        return "Unknown error occurred during preprocessing";
    }
}