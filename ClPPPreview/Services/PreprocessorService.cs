using System.Diagnostics;
using System.Text;
using ClPPPreview.Models;

namespace ClPPPreview.Services;

/// <summary>
/// Main service for orchestrating C++ preprocessing operations
/// </summary>
public class PreprocessorService : IDisposable
{
    private readonly ProcessExecutor _processExecutor;
    private readonly FileManager _fileManager;
    private readonly ConfigManager _configManager;
    private bool _disposed = false;

    public PreprocessorService(
        ProcessExecutor? processExecutor = null,
        FileManager? fileManager = null,
        ConfigManager? configManager = null)
    {
        _processExecutor = processExecutor ?? new ProcessExecutor();
        _fileManager = fileManager ?? new FileManager();
        _configManager = configManager ?? new ConfigManager();
    }

    /// <summary>
    /// Processes C++ source code through the MSVC preprocessor
    /// </summary>
    /// <param name="sourceCode">C++ source code to preprocess</param>
    /// <param name="config">Preprocessor configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preprocessing result</returns>
    public async Task<PreprocessResult> ProcessSourceAsync(
        string sourceCode,
        PreprocessorConfig config,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PreprocessorService));

        var result = new PreprocessResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Validate inputs
            ValidateInputs(sourceCode, config);

            // Create temporary source file
            var sourceFilePath = _fileManager.CreateTemporarySourceFile(sourceCode, ".cpp");

            try
            {
                // Build command line arguments including the source file
                var arguments = BuildCommandLineArguments(config, sourceFilePath);

                // Execute cl.exe with VsDevCmd environment if available
                ProcessResult processResult;
                if (!string.IsNullOrWhiteSpace(config.VsDevCmdPath) && File.Exists(config.VsDevCmdPath))
                {
                    processResult = await _processExecutor.ExecuteWithVsEnvironmentAsync(
                        config.VsDevCmdPath,
                        config.BuildToolPath,
                        arguments,
                        Path.GetDirectoryName(sourceFilePath)!,
                        cancellationToken,
                        30000); // 30 second timeout
                }
                else
                {
                    if (!File.Exists(config.BuildToolPath))
                        throw new FileNotFoundException($"Executable not found: {config.BuildToolPath}");

                    // Fallback to normal execution
                    processResult = await _processExecutor.ExecuteAsync(
                        config.BuildToolPath,
                        arguments,
                        Path.GetDirectoryName(sourceFilePath)!,
                        cancellationToken,
                        30000); // 30 second timeout
                }

                // Parse results
                result.Success = processResult.Success;
                result.Output = processResult.StandardOutput;
                result.ErrorOutput = processResult.StandardError;
                result.ExitCode = processResult.ExitCode;
                result.Duration = processResult.Duration;

                // If preprocessing was successful but no output, that might be normal
                if (result.Success && string.IsNullOrWhiteSpace(result.Output))
                {
                    result.Output = "// Preprocessing completed successfully (no output generated)";
                }

                // Clean up verbose cl.exe messages from output if successful
                if (result.Success)
                {
                    result.Output = CleanPreprocessorOutput(result.Output);
                }
            }
            finally
            {
                // Clean up temporary source file
                _fileManager.DeleteTemporaryFile(sourceFilePath);
            }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorOutput = "Preprocessing operation was cancelled";
            result.ExitCode = -1;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorOutput = $"Preprocessing failed: {ex.Message}";
            result.ExitCode = -1;
            
            // Log the full exception for debugging
            System.Diagnostics.Debug.WriteLine($"Preprocessing exception: {ex}");
        }
        finally
        {
            result.Duration = DateTime.UtcNow - result.StartTime;
        }

        return result;
    }

    /// <summary>
    /// Gets help information for cl.exe compiler
    /// </summary>
    public async Task<PreprocessResult> GetCompilerHelpAsync(
        PreprocessorConfig config,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PreprocessorService));

        var result = new PreprocessResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Validate that we have a valid cl.exe path
            if (string.IsNullOrWhiteSpace(config.BuildToolPath) || !File.Exists(config.BuildToolPath))
            {
                result.Success = false;
                result.ErrorOutput = "Invalid or missing cl.exe path. Please configure the build tool path.";
                result.ExitCode = -1;
                return result;
            }

            // Execute cl.exe with /? to get help
            ProcessResult processResult;
            if (!string.IsNullOrWhiteSpace(config.VsDevCmdPath) && File.Exists(config.VsDevCmdPath))
            {
                processResult = await _processExecutor.ExecuteWithVsEnvironmentAsync(
                    config.VsDevCmdPath,
                    config.BuildToolPath,
                    "/?",
                    Environment.CurrentDirectory,
                    cancellationToken,
                    10000); // 10 second timeout
            }
            else
            {
                processResult = await _processExecutor.ExecuteAsync(
                    config.BuildToolPath,
                    "/?",
                    Environment.CurrentDirectory,
                    cancellationToken,
                    10000); // 10 second timeout
            }

            // cl.exe help is typically output to stderr, not stdout
            result.Success = true;
            result.Output = !string.IsNullOrWhiteSpace(processResult.StandardOutput) 
                ? processResult.StandardOutput 
                : processResult.StandardError;
            result.ErrorOutput = processResult.StandardError;
            result.ExitCode = processResult.ExitCode;
            result.Duration = processResult.Duration;

            // If no output, provide a fallback message
            if (string.IsNullOrWhiteSpace(result.Output))
            {
                result.Output = "// cl.exe help information was not available\r\n// Try running 'cl.exe /?' manually in a Developer Command Prompt";
            }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorOutput = "Help operation was cancelled";
            result.ExitCode = -1;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorOutput = $"Failed to get help: {ex.Message}";
            result.ExitCode = -1;
            
            System.Diagnostics.Debug.WriteLine($"Help operation exception: {ex}");
        }
        finally
        {
            result.Duration = DateTime.UtcNow - result.StartTime;
        }

        return result;
    }

    /// <summary>
    /// Gets information about include paths being used
    /// </summary>
    public string GetIncludePathInfo(PreprocessorConfig config)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PreprocessorService));

        try
        {
            if (!config.AutoIncludePaths)
            {
                return "// Automatic include paths are disabled\r\n// Only user-specified include paths will be used";
            }

            if (string.IsNullOrWhiteSpace(config.BuildToolPath) || !File.Exists(config.BuildToolPath))
            {
                return "// Cannot determine include paths: cl.exe path is invalid";
            }

            var configManager = new ConfigManager();
            var msvcPaths = configManager.FindMsvcIncludePaths(config.BuildToolPath);
            var sdkPaths = configManager.FindWindowsSdkIncludePaths();
            var includeArgs = configManager.BuildIncludePathArguments(config.BuildToolPath);

            var info = new StringBuilder();
            info.AppendLine("// Include Path Configuration");
            info.AppendLine($"// Auto Include Paths: {(config.AutoIncludePaths ? "Enabled" : "Disabled")}");
            info.AppendLine($"// cl.exe Path: {config.BuildToolPath}");
            info.AppendLine();

            info.AppendLine("// MSVC Include Paths:");
            foreach (var path in msvcPaths)
            {
                info.AppendLine($"//   {path}");
            }

            info.AppendLine();
            info.AppendLine("// Windows SDK Include Paths:");
            foreach (var path in sdkPaths)
            {
                info.AppendLine($"//   {path}");
            }

            info.AppendLine();
            info.AppendLine("// Generated Include Arguments:");
            info.AppendLine($"// {includeArgs}");

            return info.ToString();
        }
        catch (Exception ex)
        {
            return $"// Error getting include path info: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates the build tool path in the configuration
    /// </summary>
    /// <param name="toolPath">Path to cl.exe</param>
    /// <returns>True if path is valid</returns>
    public bool ValidateBuildToolPath(string toolPath)
    {
        if (string.IsNullOrWhiteSpace(toolPath))
            return false;

        return _fileManager.ValidateExecutablePath(toolPath) &&
               Path.GetFileName(toolPath).Equals("cl.exe", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the default command line arguments for preprocessing
    /// </summary>
    /// <returns>Array of default arguments</returns>
    public string[] GetDefaultCommandLineArgs()
    {
        return new[]
        {
            "/EP",  // Preprocess to stdout, preserve comments
            "/C"    // Preserve comments during preprocessing
        };
    }

    /// <summary>
    /// Gets recommended command line arguments with descriptions
    /// </summary>
    /// <returns>Dictionary of arguments and their descriptions</returns>
    public Dictionary<string, string> GetRecommendedArguments()
    {
        return new Dictionary<string, string>
        {
            ["/EP"] = "Preprocess to stdout without #line directives",
            ["/E"] = "Preprocess to stdout with #line directives",
            ["/C"] = "Preserve comments during preprocessing",
            ["/FI <file>"] = "Force include of specified file",
            ["/I <dir>"] = "Add directory to include search path",
            ["/D <macro>=<value>"] = "Define preprocessor macro",
            ["/U <macro>"] = "Undefine preprocessor macro",
            ["/showIncludes"] = "Display include file names",
            ["/nologo"] = "Suppress copyright message",
            ["/TC"] = "Treat all source files as C",
            ["/TP"] = "Treat all source files as C++",
            ["/std:c++17"] = "Set C++ language standard to C++17",
            ["/std:c++20"] = "Set C++ language standard to C++20"
        };
    }

    /// <summary>
    /// Attempts to auto-detect the MSVC installation
    /// </summary>
    /// <returns>Path to cl.exe if found, otherwise null</returns>
    public string? AutoDetectMSVCPath()
    {
        var path = _configManager.FindMSVCToolPath();
        return string.IsNullOrEmpty(path) ? null : path;
    }

    /// <summary>
    /// Tests if the specified cl.exe path works by running a simple command
    /// </summary>
    /// <param name="clPath">Path to cl.exe to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cl.exe responds correctly</returns>
    public async Task<bool> TestBuildToolAsync(string clPath, CancellationToken cancellationToken = default)
    {
        if (!ValidateBuildToolPath(clPath))
            return false;

        try
        {
            // Test by running cl.exe with /help flag
            var result = await _processExecutor.ExecuteAsync(
                clPath,
                "/help",
                string.Empty,
                cancellationToken,
                5000); // 5 second timeout for help command

            // cl.exe should return with exit code 0 for /help
            return result.ExitCode == 0 && 
                   (result.StandardOutput.Contains("Microsoft") || result.StandardError.Contains("Microsoft"));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets version information for the specified cl.exe
    /// </summary>
    /// <param name="clPath">Path to cl.exe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version string if available</returns>
    public async Task<string?> GetCompilerVersionAsync(string clPath, CancellationToken cancellationToken = default)
    {
        if (!ValidateBuildToolPath(clPath))
            return null;

        try
        {
            // Get version by running cl.exe with no arguments (it prints version to stderr)
            var result = await _processExecutor.ExecuteAsync(
                clPath,
                string.Empty,
                string.Empty,
                cancellationToken,
                3000); // 3 second timeout

            var output = result.StandardError + " " + result.StandardOutput;
            
            // Extract version information
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var versionLine = lines.FirstOrDefault(l => 
                l.Contains("Microsoft") && (l.Contains("C/C++") || l.Contains("Compiler")));

            return versionLine?.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates input parameters for preprocessing
    /// </summary>
    /// <param name="sourceCode">Source code to validate</param>
    /// <param name="config">Configuration to validate</param>
    private void ValidateInputs(string sourceCode, PreprocessorConfig config)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (!config.IsValid())
            throw new ArgumentException("Configuration is not valid", nameof(config));

        if (!ValidateBuildToolPath(config.BuildToolPath))
            throw new ArgumentException($"Invalid build tool path: {config.BuildToolPath}", nameof(config));
    }

    /// <summary>
    /// Builds the command line arguments for cl.exe
    /// </summary>
    /// <param name="userArgs">User-specified arguments</param>
    /// <param name="sourceFile">Path to source file</param>
    /// <returns>Complete argument string</returns>
    private string BuildCommandLineArguments(PreprocessorConfig config, string sourceFile)
    {
        var args = new List<string>();

        // Always add /nologo to reduce noise
        args.Add("/nologo");

        // Add automatic include paths if enabled
        if (config.AutoIncludePaths && !string.IsNullOrWhiteSpace(config.BuildToolPath))
        {
            var configManager = new ConfigManager();
            var includePathArgs = configManager.BuildIncludePathArguments(config.BuildToolPath);
            if (!string.IsNullOrWhiteSpace(includePathArgs))
            {
                // Split include arguments and add them
                var includeArgsList = SplitCommandLineArgs(includePathArgs);
                args.AddRange(includeArgsList);
            }
        }

        // Add user arguments if specified
        if (!string.IsNullOrWhiteSpace(config.CommandLineArgs))
        {
            // Split user arguments properly (respecting quoted strings)
            var userArgsList = SplitCommandLineArgs(config.CommandLineArgs);
            args.AddRange(userArgsList);
        }
        else
        {
            // Default preprocessing arguments
            args.Add("/EP"); // Preprocess only
            args.Add("/C");  // Preserve comments
        }

        // Add source file as the last argument
        args.Add(QuoteArgumentIfNeeded(sourceFile));

        return string.Join(" ", args.Select(QuoteArgumentIfNeeded));
    }

    /// <summary>
    /// Splits command line arguments properly, respecting quoted strings
    /// </summary>
    /// <param name="commandLine">Command line to split</param>
    /// <returns>Array of individual arguments</returns>
    private static string[] SplitCommandLineArgs(string commandLine)
    {
        var args = new List<string>();
        var currentArg = new StringBuilder();
        var inQuotes = false;
        var escapeNext = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if (escapeNext)
            {
                currentArg.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
            {
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (currentArg.Length > 0)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                }
                continue;
            }

            currentArg.Append(c);
        }

        if (currentArg.Length > 0)
        {
            args.Add(currentArg.ToString());
        }

        return args.ToArray();
    }

    /// <summary>
    /// Quotes an argument if it contains spaces or special characters
    /// </summary>
    /// <param name="argument">Argument to potentially quote</param>
    /// <returns>Quoted argument if necessary</returns>
    private static string QuoteArgumentIfNeeded(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "\"\"";

        if (argument.Contains(' ') || argument.Contains('\t') || argument.Contains('"'))
        {
            return "\"" + argument.Replace("\"", "\\\"") + "\"";
        }

        return argument;
    }

    /// <summary>
    /// Cleans up verbose output from cl.exe preprocessing
    /// </summary>
    /// <param name="output">Raw preprocessor output</param>
    /// <returns>Cleaned output</returns>
    private static string CleanPreprocessorOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return output;

        var lines = output.Split('\n', StringSplitOptions.None);
        var cleanedLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip Microsoft copyright lines and other noise
            if (trimmed.StartsWith("Microsoft") ||
                trimmed.Contains("Copyright") ||
                trimmed.Contains("(R)") ||
                string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            cleanedLines.Add(line);
        }

        return string.Join('\n', cleanedLines);
    }

    /// <summary>
    /// Disposes of the service and its dependencies
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _processExecutor?.Dispose();
            _fileManager?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to ensure cleanup
    /// </summary>
    ~PreprocessorService()
    {
        Dispose();
    }
}