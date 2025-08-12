using System.Diagnostics;
using System.Text;
using ClPPPreview.Models;

namespace ClPPPreview.Services;

/// <summary>
/// Service for safely executing external processes with proper resource management
/// </summary>
public class ProcessExecutor : IDisposable
{
    private readonly List<Process> _runningProcesses = new();
    private readonly object _processLock = new object();
    private bool _disposed = false;

    /// <summary>
    /// Executes an external process asynchronously
    /// </summary>
    /// <param name="executable">Path to the executable</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="workingDirectory">Working directory for the process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default 30 seconds)</param>
    /// <returns>Process execution result</returns>
    public async Task<ProcessResult> ExecuteAsync(
        string executable,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default,
        int timeoutMs = 30000)
    {
        if (string.IsNullOrWhiteSpace(executable))
            throw new ArgumentException("Executable path cannot be null or empty", nameof(executable));

        if (!File.Exists(executable))
            throw new FileNotFoundException($"Executable not found: {executable}");

        Process? process = null;
        var result = new ProcessResult();
        var startTime = DateTime.UtcNow;

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            process = new Process { StartInfo = processStartInfo };

            // Track running processes for cleanup
            lock (_processLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ProcessExecutor));
                _runningProcesses.Add(process);
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // Set up async output reading
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            Debug.WriteLine($"Starting process: \"{executable}\" {arguments}");

            // Start the process
            if (!process.Start())
                throw new InvalidOperationException("Failed to start process");


            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout and cancellation
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(combinedCts.Token);
                result.ExitCode = process.ExitCode;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // Timeout occurred
                result.WasCancelled = true;
                result.ExitCode = -1;
                result.Exception = new TimeoutException($"Process timed out after {timeoutMs}ms");

                TryKillProcess(process);
            }
            catch (OperationCanceledException)
            {
                // User cancellation
                result.WasCancelled = true;
                result.ExitCode = -1;

                TryKillProcess(process);
            }

            // Wait a bit for final output to be captured
            await Task.Delay(100, CancellationToken.None);

            result.StandardOutput = outputBuilder.ToString();
            result.StandardError = errorBuilder.ToString();
            result.Duration = DateTime.UtcNow - startTime;
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.ExitCode = -1;
            result.Duration = DateTime.UtcNow - startTime;
        }
        finally
        {
            if (process != null)
            {
                lock (_processLock)
                {
                    _runningProcesses.Remove(process);
                }

                try
                {
                    if (!process.HasExited)
                        TryKillProcess(process);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Executes a command with Visual Studio environment setup via VsDevCmd.bat
    /// </summary>
    public async Task<ProcessResult> ExecuteWithVsEnvironmentAsync(
        string vsDevCmdPath,
        string executable,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default,
        int timeoutMs = 30000)
    {
        if (string.IsNullOrWhiteSpace(vsDevCmdPath) || !File.Exists(vsDevCmdPath))
        {
            // Fallback to normal execution if VsDevCmd.bat is not available
            return await ExecuteAsync(executable, arguments, workingDirectory, cancellationToken, timeoutMs);
        }

        // Create a batch command that runs VsDevCmd.bat first, then the target command
        var batchCommand = $"\"\"{vsDevCmdPath}\" && \"{executable}\" {arguments}\"";
        
        return await ExecuteAsync("cmd.exe", $"/c {batchCommand}", workingDirectory, cancellationToken, timeoutMs);
    }

    /// <summary>
    /// Executes a simple command and returns only the output
    /// </summary>
    /// <param name="executable">Path to executable</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Standard output from the process</returns>
    public async Task<string> ExecuteSimpleAsync(
        string executable,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(executable, arguments, string.Empty, cancellationToken);

        if (!result.Success && result.Exception != null)
            throw result.Exception;

        return result.StandardOutput;
    }

    /// <summary>
    /// Kills all running processes managed by this executor
    /// </summary>
    public void KillAllProcesses()
    {
        lock (_processLock)
        {
            foreach (var process in _runningProcesses.ToList())
            {
                TryKillProcess(process);
            }
            _runningProcesses.Clear();
        }
    }

    /// <summary>
    /// Attempts to gracefully kill a process
    /// </summary>
    /// <param name="process">Process to kill</param>
    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                // Try graceful shutdown first
                process.CloseMainWindow();

                // Wait briefly for graceful shutdown
                if (!process.WaitForExit(1000))
                {
                    // Force kill if graceful shutdown failed
                    process.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - cleanup should be best effort
            System.Diagnostics.Debug.WriteLine($"Failed to kill process: {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes of the ProcessExecutor and kills any running processes
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            KillAllProcesses();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to ensure cleanup
    /// </summary>
    ~ProcessExecutor()
    {
        Dispose();
    }
}