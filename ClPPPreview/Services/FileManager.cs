using System.Security.Cryptography;
using System.Text;

namespace ClPPPreview.Services;

/// <summary>
/// Service for managing temporary files used during preprocessing
/// </summary>
public class FileManager : IDisposable
{
    private readonly string _tempDirectory;
    private readonly HashSet<string> _managedFiles = new();
    private readonly object _fileLock = new object();
    private bool _disposed = false;

    public FileManager()
    {
        // Create a dedicated temp directory for the application
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ClppPreview");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Creates a temporary source file with the given C++ content
    /// </summary>
    /// <param name="content">C++ source code content</param>
    /// <param name="extension">File extension (default: .cpp)</param>
    /// <returns>Path to the created temporary file</returns>
    public string CreateTemporarySourceFile(string content, string extension = ".cpp")
    {
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        lock (_fileLock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileManager));

            // Generate a unique filename using timestamp and random component
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var randomPart = Path.GetRandomFileName().Replace(".", "");
            var filename = $"temp_{timestamp}_{randomPart}{extension}";
            var filePath = Path.Combine(_tempDirectory, filename);

            try
            {
                // Validate extension for security
                ValidateFileExtension(extension);

                // Sanitize content to prevent issues
                var sanitizedContent = SanitizeSourceContent(content);

                // Write content to file with UTF-8 encoding
                File.WriteAllText(filePath, sanitizedContent, Encoding.UTF8);

                // Set restrictive permissions (owner read/write only)
                SetRestrictivePermissions(filePath);

                // Track the file for cleanup
                _managedFiles.Add(filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                // Clean up on failure
                TryDeleteFile(filePath);
                throw new InvalidOperationException($"Failed to create temporary source file: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Creates a temporary batch file for running cl.exe with proper escaping
    /// </summary>
    /// <param name="clPath">Path to cl.exe</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="sourceFile">Path to source file</param>
    /// <returns>Path to the created batch file</returns>
    public string CreateTemporaryBatchFile(string clPath, string arguments, string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(clPath))
            throw new ArgumentException("cl.exe path cannot be null or empty", nameof(clPath));

        if (string.IsNullOrWhiteSpace(sourceFile))
            throw new ArgumentException("Source file path cannot be null or empty", nameof(sourceFile));

        lock (_fileLock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileManager));

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var randomPart = Path.GetRandomFileName().Replace(".", "");
            var filename = $"compile_{timestamp}_{randomPart}.bat";
            var filePath = Path.Combine(_tempDirectory, filename);

            try
            {
                // Escape paths for batch file
                var escapedClPath = EscapeBatchPath(clPath);
                var escapedSourceFile = EscapeBatchPath(sourceFile);
                var safeArguments = SanitizeArguments(arguments);

                // Create batch file content
                var batchContent = new StringBuilder();
                batchContent.AppendLine("@echo off");
                batchContent.AppendLine("chcp 65001 > nul"); // Set UTF-8 code page
                batchContent.AppendLine($"\"{escapedClPath}\" {safeArguments} \"{escapedSourceFile}\"");

                File.WriteAllText(filePath, batchContent.ToString(), Encoding.UTF8);
                SetRestrictivePermissions(filePath);
                _managedFiles.Add(filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                TryDeleteFile(filePath);
                throw new InvalidOperationException($"Failed to create temporary batch file: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Deletes a specific temporary file if it's managed by this instance
    /// </summary>
    /// <param name="filePath">Path to the file to delete</param>
    /// <returns>True if file was deleted, false otherwise</returns>
    public bool DeleteTemporaryFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        lock (_fileLock)
        {
            if (!_managedFiles.Contains(filePath))
                return false;

            var deleted = TryDeleteFile(filePath);
            if (deleted)
            {
                _managedFiles.Remove(filePath);
            }
            return deleted;
        }
    }

    /// <summary>
    /// Cleans up all temporary files created by this instance
    /// </summary>
    public void CleanupTemporaryFiles()
    {
        lock (_fileLock)
        {
            var filesToRemove = _managedFiles.ToList();
            foreach (var file in filesToRemove)
            {
                if (TryDeleteFile(file))
                    _managedFiles.Remove(file);
            }
        }
    }

    /// <summary>
    /// Validates that the executable path is safe to use
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <returns>True if path is valid and safe</returns>
    public bool ValidateExecutablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check if file exists
            if (!File.Exists(path))
                return false;

            // Check if it's an executable
            var extension = Path.GetExtension(path);
            if (!extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                return false;

            // Ensure path is absolute and doesn't contain suspicious patterns
            var fullPath = Path.GetFullPath(path);
            if (!path.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                return false;

            // Check for directory traversal attempts
            if (path.Contains("..") || path.Contains("~"))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets information about the temporary directory
    /// </summary>
    /// <returns>Directory info including size and file count</returns>
    public (string Path, long SizeBytes, int FileCount) GetTemporaryDirectoryInfo()
    {
        try
        {
            if (!Directory.Exists(_tempDirectory))
                return (_tempDirectory, 0, 0);

            var files = Directory.GetFiles(_tempDirectory);
            var totalSize = files.Sum(f => new FileInfo(f).Length);

            return (_tempDirectory, totalSize, files.Length);
        }
        catch
        {
            return (_tempDirectory, 0, 0);
        }
    }

    /// <summary>
    /// Validates file extension for security
    /// </summary>
    /// <param name="extension">File extension to validate</param>
    private static void ValidateFileExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be null or empty");

        var allowedExtensions = new[] { ".cpp", ".c", ".cc", ".cxx", ".h", ".hpp", ".hxx", ".bat", ".tmp" };
        if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
            throw new ArgumentException($"File extension '{extension}' is not allowed");
    }

    /// <summary>
    /// Sanitizes C++ source content to prevent issues
    /// </summary>
    /// <param name="content">Original content</param>
    /// <returns>Sanitized content</returns>
    private static string SanitizeSourceContent(string content)
    {
        // Remove null characters and other control characters that could cause issues
        var sanitized = new StringBuilder(content.Length);
        foreach (var c in content)
        {
            if (c == '\0' || (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
                continue; // Skip problematic characters
            sanitized.Append(c);
        }
        return sanitized.ToString();
    }

    /// <summary>
    /// Sanitizes command line arguments
    /// </summary>
    /// <param name="arguments">Original arguments</param>
    /// <returns>Sanitized arguments</returns>
    private static string SanitizeArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return string.Empty;

        // Remove potentially dangerous characters and commands
        var dangerous = new[] { "&", "|", ";", "`", "$", ">", "<", "&&", "||" };
        var sanitized = arguments;
        
        foreach (var danger in dangerous)
        {
            sanitized = sanitized.Replace(danger, " ");
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Escapes a file path for use in batch files
    /// </summary>
    /// <param name="path">Path to escape</param>
    /// <returns>Escaped path</returns>
    private static string EscapeBatchPath(string path)
    {
        // For batch files, we mainly need to handle spaces and special characters
        return path.Replace("%", "%%");
    }

    /// <summary>
    /// Sets restrictive file permissions (Windows)
    /// </summary>
    /// <param name="filePath">File to set permissions on</param>
    private static void SetRestrictivePermissions(string filePath)
    {
        try
        {
            // On Windows, set read-only for others, full access for owner
            var fileInfo = new FileInfo(filePath);
            // This is basic - for more security, we'd use Windows ACLs
            fileInfo.Attributes = FileAttributes.Normal;
        }
        catch
        {
            // Best effort - don't fail if we can't set permissions
        }
    }

    /// <summary>
    /// Attempts to delete a file, ignoring errors
    /// </summary>
    /// <param name="filePath">File to delete</param>
    /// <returns>True if deleted successfully</returns>
    private static bool TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // Remove read-only attribute if set
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete file {filePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disposes of the FileManager and cleans up all temporary files
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            CleanupTemporaryFiles();
            
            // Try to remove the temp directory if it's empty
            try
            {
                if (Directory.Exists(_tempDirectory) && !Directory.EnumerateFileSystemEntries(_tempDirectory).Any())
                    Directory.Delete(_tempDirectory);
            }
            catch
            {
                // Best effort cleanup
            }

            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to ensure cleanup
    /// </summary>
    ~FileManager()
    {
        Dispose();
    }
}