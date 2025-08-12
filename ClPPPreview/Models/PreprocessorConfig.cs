using System.ComponentModel.DataAnnotations;

namespace ClPPPreview.Models;

/// <summary>
/// Configuration settings for the MSVC preprocessor
/// </summary>
public class PreprocessorConfig
{
    /// <summary>
    /// Path to the cl.exe build tool
    /// </summary>
    [Required]
    public string BuildToolPath { get; set; } = string.Empty;
    /// <summary>
    /// Path to VsDevCmd.bat for setting up Visual Studio environment
    /// </summary>
    public string VsDevCmdPath { get; set; } = string.Empty;
    /// <summary>
    /// Whether to automatically add standard MSVC and Windows SDK include paths
    /// </summary>
    public bool AutoIncludePaths { get; set; } = true;

    /// <summary>
    /// Command line arguments to pass to cl.exe
    /// </summary>
    public string CommandLineArgs { get; set; } =
        "/EP /C /Zc:preprocessor /permissive- /D \"_DEBUG\" /D \"_WINDOWS\" /D \"_UNICODE\" /D \"UNICODE\" ";

    /// <summary>
    /// Debounce delay in milliseconds after user stops typing
    /// </summary>
    [Range(100, 5000)]
    public int DebounceDelayMs { get; set; } = 500;

    /// <summary>
    /// Main window width
    /// </summary>
    [Range(400, 3840)]
    public int WindowWidth { get; set; } = 913;

    /// <summary>
    /// Main window height
    /// </summary>
    [Range(300, 2160)]
    public int WindowHeight { get; set; } = 936;

    /// <summary>
    /// Splitter panel distance
    /// </summary>
    [Range(100, 2000)]
    public int SplitterDistance { get; set; } = 448;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(BuildToolPath) &&
               DebounceDelayMs >= 100 && DebounceDelayMs <= 5000 &&
               WindowWidth >= 400 && WindowHeight >= 300;
    }
}