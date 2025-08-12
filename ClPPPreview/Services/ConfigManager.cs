using System.Text.Json;
using System.Text.RegularExpressions;
using ClPPPreview.Models;
using Microsoft.Win32;

namespace ClPPPreview.Services;

/// <summary>
/// Manages application configuration and settings persistence
/// </summary>
public class ConfigManager
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private PreprocessorConfig? _cachedConfig;

    public ConfigManager()
    {
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClppPreview");
        _configFilePath = Path.Combine(_configDirectory, "settings.json");
    }

    /// <summary>
    /// Loads configuration from file or returns default configuration
    /// </summary>
    /// <returns>Configuration object</returns>
    public PreprocessorConfig LoadConfig()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<PreprocessorConfig>(json);
                if (config != null && config.IsValid())
                {
                    _cachedConfig = config;
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            // Log error and fall back to default config
            System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
        }

        // Return default configuration
        var defaultConfig = CreateDefaultConfig();
        _cachedConfig = defaultConfig;
        return defaultConfig;
    }

    /// <summary>
    /// Saves configuration to file
    /// </summary>
    /// <param name="config">Configuration to save</param>
    public void SaveConfig(PreprocessorConfig config)
    {
        try
        {
            if (!config.IsValid())
                throw new ArgumentException("Invalid configuration");

            // Ensure directory exists
            Directory.CreateDirectory(_configDirectory);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configFilePath, json);

            _cachedConfig = config;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to find MSVC cl.exe tool path automatically
    /// </summary>
    /// <returns>Path to cl.exe if found, otherwise empty string</returns>
    public string FindMSVCToolPath()
    {
        // Common installation paths to check
        var commonPaths = new[]
        {
            // Visual Studio 2022
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC",
            @"C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC",
            @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\VC\Tools\MSVC",
            
            // Visual Studio 2019
            @"C:\Program Files\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC",
            @"C:\Program Files\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC",
            @"C:\Program Files\Microsoft Visual Studio\2019\Enterprise\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Tools\MSVC",

            // Build Tools
            @"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Tools\MSVC",
            @"C:\Program Files\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC"
        };

        foreach (var basePath in commonPaths)
        {
            try
            {
                if (Directory.Exists(basePath))
                {
                    // Find the latest version directory
                    var versionDirs = Directory.GetDirectories(basePath)
                        .OrderByDescending(d => new DirectoryInfo(d).Name)
                        .ToArray();

                    foreach (var versionDir in versionDirs)
                    {
                        // Check for cl.exe in the x64 host tools
                        var clPath = Path.Combine(versionDir, "bin", "Hostx64", "x64", "cl.exe");
                        if (File.Exists(clPath))
                            return clPath;

                        // Check for cl.exe in the x86 host tools
                        clPath = Path.Combine(versionDir, "bin", "Hostx86", "x86", "cl.exe");
                        if (File.Exists(clPath))
                            return clPath;
                    }
                }
            }
            catch (Exception ex)
            {
                // Continue searching if this path fails
                System.Diagnostics.Debug.WriteLine($"Failed to check path {basePath}: {ex.Message}");
            }
        }

        // Try to find cl.exe in PATH
        try
        {
            var pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVariable))
            {
                var paths = pathVariable.Split(Path.PathSeparator);
                foreach (var path in paths)
                {
                    var clPath = Path.Combine(path, "cl.exe");
                    if (File.Exists(clPath))
                        return clPath;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to search PATH: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    /// Attempts to find VsDevCmd.bat for setting up Visual Studio environment
    /// </summary>
    /// <returns>Path to VsDevCmd.bat if found, otherwise empty string</returns>
    public string FindVsDevCmdPath()
    {
        // Common installation paths for VsDevCmd.bat
        var commonPaths = new[]
        {
            // Visual Studio 2022
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\VsDevCmd.bat",
            
            // Visual Studio 2019
            @"C:\Program Files\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files\Microsoft Visual Studio\2019\Professional\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat",

            // Build Tools
            @"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files\Microsoft Visual Studio\2019\BuildTools\Common7\Tools\VsDevCmd.bat",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\Common7\Tools\VsDevCmd.bat"
        };

        foreach (var path in commonPaths)
        {
            try
            {
                if (File.Exists(path))
                    return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check VsDevCmd path {path}: {ex.Message}");
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Finds Windows SDK include paths
    /// </summary>
    /// <returns>List of Windows SDK include paths</returns>
    public List<string> FindWindowsSdkIncludePaths()
    {
        var includePaths = new List<string>();

        try
        {
            // Common Windows SDK locations
            var sdkBasePaths = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\Include",
                @"C:\Program Files\Windows Kits\10\Include",
                @"C:\Program Files (x86)\Windows Kits\8.1\Include",
                @"C:\Program Files\Windows Kits\8.1\Include"
            };

            foreach (var basePath in sdkBasePaths)
            {
                if (Directory.Exists(basePath))
                {
                    // For Windows 10 SDK, find the latest version
                    if (basePath.Contains("10"))
                    {
                        var versionDirs = Directory.GetDirectories(basePath)
                            .Where(d => Regex.IsMatch(Path.GetFileName(d), @"10\.\d+\.\d+\.\d+"))
                            .OrderByDescending(d => new Version(Path.GetFileName(d)))
                            .ToArray();

                        foreach (var versionDir in versionDirs.Take(1)) // Use latest version
                        {
                            var ucrtPath = Path.Combine(versionDir, "ucrt");
                            var umPath = Path.Combine(versionDir, "um");
                            var sharedPath = Path.Combine(versionDir, "shared");
                            var winrtPath = Path.Combine(versionDir, "winrt");
                            var cppwinrtPath = Path.Combine(versionDir, "cppwinrt");

                            if (Directory.Exists(ucrtPath)) includePaths.Add(ucrtPath);
                            if (Directory.Exists(umPath)) includePaths.Add(umPath);
                            if (Directory.Exists(sharedPath)) includePaths.Add(sharedPath);
                            if (Directory.Exists(winrtPath)) includePaths.Add(winrtPath);
                            if (Directory.Exists(cppwinrtPath)) includePaths.Add(cppwinrtPath);
                        }
                    }
                    else // Windows 8.1 SDK
                    {
                        var includePath = Path.Combine(basePath, "um");
                        var sharedPath = Path.Combine(basePath, "shared");
                        
                        if (Directory.Exists(includePath)) includePaths.Add(includePath);
                        if (Directory.Exists(sharedPath)) includePaths.Add(sharedPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error finding Windows SDK paths: {ex.Message}");
        }

        return includePaths;
    }

    /// <summary>
    /// Finds MSVC toolset include paths based on cl.exe path
    /// </summary>
    /// <param name="clExePath">Path to cl.exe</param>
    /// <returns>List of MSVC include paths</returns>
    public List<string> FindMsvcIncludePaths(string clExePath)
    {
        var includePaths = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(clExePath) || !File.Exists(clExePath))
                return includePaths;

            // Navigate from cl.exe path to find include directories
            // Typical path: VC\Tools\MSVC\{version}\bin\Hostx64\x64\cl.exe
            var clDir = Path.GetDirectoryName(clExePath);
            if (clDir != null)
            {
                // Go up to the MSVC version directory
                var parentDir = Directory.GetParent(clDir)?.Parent?.Parent;
                if (parentDir != null && parentDir.Exists)
                {
                    var includePath = Path.Combine(parentDir.FullName, "include");
                    var atlmfcPath = Path.Combine(parentDir.FullName, "atlmfc", "include");

                    if (Directory.Exists(includePath))
                        includePaths.Add(includePath);
                    if (Directory.Exists(atlmfcPath))
                        includePaths.Add(atlmfcPath);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error finding MSVC include paths: {ex.Message}");
        }

        return includePaths;
    }

    /// <summary>
    /// Builds complete include path arguments for cl.exe
    /// </summary>
    /// <param name="clExePath">Path to cl.exe</param>
    /// <returns>String containing all /I arguments</returns>
    public string BuildIncludePathArguments(string clExePath)
    {
        var includeArgs = new List<string>();

        try
        {
            // Add MSVC include paths
            var msvcPaths = FindMsvcIncludePaths(clExePath);
            foreach (var path in msvcPaths)
            {
                includeArgs.Add($"/I\"{path}\"");
            }

            // Add Windows SDK include paths
            var sdkPaths = FindWindowsSdkIncludePaths();
            foreach (var path in sdkPaths)
            {
                includeArgs.Add($"/I\"{path}\"");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error building include path arguments: {ex.Message}");
        }

        return string.Join(" ", includeArgs);
    }

    /// <summary>
    /// Validates that the specified executable path exists and is executable
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <returns>True if path is valid</returns>
    public bool ValidateExecutablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            return File.Exists(path) && 
                   Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates default configuration with automatic tool path detection
    /// </summary>
    /// <returns>Default configuration</returns>
    private PreprocessorConfig CreateDefaultConfig()
    {
        var config = new PreprocessorConfig();
        
        // Try to find MSVC automatically
        var autoPath = FindMSVCToolPath();
        if (!string.IsNullOrEmpty(autoPath))
        {
            config.BuildToolPath = autoPath;
        }

        // Try to find VsDevCmd.bat automatically
        var vsDevCmdPath = FindVsDevCmdPath();
        if (!string.IsNullOrEmpty(vsDevCmdPath))
        {
            config.VsDevCmdPath = vsDevCmdPath;
        }

        return config;
    }
}