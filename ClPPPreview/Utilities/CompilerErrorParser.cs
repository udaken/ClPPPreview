using System.Text.RegularExpressions;

namespace ClPPPreview.Utilities;

/// <summary>
/// Represents a compiler error or warning
/// </summary>
public class CompilerError
{
    public string FileName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsWarning { get; set; }
    public string FullText { get; set; } = string.Empty;
}

/// <summary>
/// Parses compiler error messages from cl.exe output
/// </summary>
public static class CompilerErrorParser
{
    // Pattern to match MSVC compiler errors and warnings
    // Format: filename(line,column): error/warning C####: message
    private static readonly Regex ErrorPattern = new Regex(
        @"^(?<file>[^(]+)\((?<line>\d+)(?:,(?<column>\d+))?\)\s*:\s*(?<type>error|warning)\s*(?<code>C\d+)?\s*:\s*(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    // Pattern for temporary files created by our application
    private static readonly Regex TempFilePattern = new Regex(
        @"[^\\]+\.cpp$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses compiler output and extracts error and warning information
    /// </summary>
    /// <param name="compilerOutput">The output from cl.exe</param>
    /// <returns>List of parsed compiler errors and warnings</returns>
    public static List<CompilerError> ParseErrors(string compilerOutput)
    {
        var errors = new List<CompilerError>();
        
        if (string.IsNullOrWhiteSpace(compilerOutput))
            return errors;

        try
        {
            var matches = ErrorPattern.Matches(compilerOutput);
            
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var error = new CompilerError
                    {
                        FileName = match.Groups["file"].Value.Trim(),
                        LineNumber = int.TryParse(match.Groups["line"].Value, out var line) ? line : 0,
                        ColumnNumber = int.TryParse(match.Groups["column"].Value, out var col) ? col : 0,
                        ErrorCode = match.Groups["code"].Value.Trim(),
                        Message = match.Groups["message"].Value.Trim(),
                        IsWarning = match.Groups["type"].Value.Equals("warning", StringComparison.OrdinalIgnoreCase),
                        FullText = match.Value
                    };

                    // Only include errors from temporary files (our source code)
                    if (string.IsNullOrWhiteSpace(error.FileName) || TempFilePattern.IsMatch(error.FileName))
                    {
                        errors.Add(error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing compiler output: {ex.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Extracts line numbers from the list of errors
    /// </summary>
    /// <param name="errors">List of compiler errors</param>
    /// <param name="errorsOnly">If true, only returns line numbers for errors (not warnings)</param>
    /// <returns>List of line numbers with errors</returns>
    public static List<int> GetErrorLineNumbers(IEnumerable<CompilerError> errors, bool errorsOnly = true)
    {
        return errors
            .Where(e => e.LineNumber > 0 && (!errorsOnly || !e.IsWarning))
            .Select(e => e.LineNumber)
            .Distinct()
            .OrderBy(line => line)
            .ToList();
    }

    /// <summary>
    /// Formats error messages for display
    /// </summary>
    /// <param name="errors">List of compiler errors</param>
    /// <returns>Formatted error message string</returns>
    public static string FormatErrorsForDisplay(IEnumerable<CompilerError> errors)
    {
        var errorList = errors.ToList();
        if (!errorList.Any())
            return "// No compilation errors or warnings";

        var result = new System.Text.StringBuilder();
        result.AppendLine("// Compilation Results:");
        result.AppendLine();

        var errorCount = errorList.Count(e => !e.IsWarning);
        var warningCount = errorList.Count(e => e.IsWarning);

        result.AppendLine($"// Errors: {errorCount}, Warnings: {warningCount}");
        result.AppendLine();

        foreach (var error in errorList.OrderBy(e => e.LineNumber).ThenBy(e => e.IsWarning))
        {
            var prefix = error.IsWarning ? "Warning" : "Error";
            result.AppendLine($"// {prefix} (Line {error.LineNumber}): {error.Message}");
            
            if (!string.IsNullOrWhiteSpace(error.ErrorCode))
            {
                result.AppendLine($"//   Code: {error.ErrorCode}");
            }
            
            result.AppendLine();
        }

        return result.ToString();
    }

    /// <summary>
    /// Creates a summary of compilation results
    /// </summary>
    /// <param name="errors">List of compiler errors</param>
    /// <returns>Summary string for status display</returns>
    public static string CreateErrorSummary(IEnumerable<CompilerError> errors)
    {
        var errorList = errors.ToList();
        if (!errorList.Any())
            return "No errors or warnings";

        var errorCount = errorList.Count(e => !e.IsWarning);
        var warningCount = errorList.Count(e => e.IsWarning);

        if (errorCount > 0 && warningCount > 0)
            return $"{errorCount} error(s), {warningCount} warning(s)";
        else if (errorCount > 0)
            return $"{errorCount} error(s)";
        else
            return $"{warningCount} warning(s)";
    }
}