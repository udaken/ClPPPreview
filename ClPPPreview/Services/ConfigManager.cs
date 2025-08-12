using System.Text.Json;
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

        return config;
    }
}