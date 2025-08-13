using System.Drawing;
using System.Text.Json.Serialization;

namespace ClPPPreview.Models;

/// <summary>
/// Configuration for syntax highlighting colors and themes
/// </summary>
public class SyntaxHighlightingConfig
{
    /// <summary>
    /// Color for C++ keywords (if, for, class, etc.)
    /// </summary>
    public string KeywordColor { get; set; } = "#0000FF"; // Blue

    /// <summary>
    /// Color for data types (int, string, etc.)
    /// </summary>
    public string TypeColor { get; set; } = "#008B8B"; // DarkCyan

    /// <summary>
    /// Color for string literals
    /// </summary>
    public string StringColor { get; set; } = "#A52A2A"; // Brown

    /// <summary>
    /// Color for comments
    /// </summary>
    public string CommentColor { get; set; } = "#008000"; // Green

    /// <summary>
    /// Color for preprocessor directives
    /// </summary>
    public string PreprocessorColor { get; set; } = "#808080"; // Gray

    /// <summary>
    /// Color for numeric literals
    /// </summary>
    public string NumberColor { get; set; } = "#8B008B"; // DarkMagenta

    /// <summary>
    /// Color for error highlighting
    /// </summary>
    public string ErrorColor { get; set; } = "#FF0000"; // Red

    /// <summary>
    /// Color for normal text
    /// </summary>
    public string NormalColor { get; set; } = "#000000"; // Black

    /// <summary>
    /// Whether syntax highlighting is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to use bold font for keywords
    /// </summary>
    public bool BoldKeywords { get; set; } = true;

    /// <summary>
    /// Whether to use italic font for comments
    /// </summary>
    public bool ItalicComments { get; set; } = true;

    /// <summary>
    /// Current theme name
    /// </summary>
    public string ThemeName { get; set; } = "Default";

    /// <summary>
    /// Converts hex color string to Color object
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, Color> ColorScheme => new()
    {
        { "keyword", ColorTranslator.FromHtml(KeywordColor) },
        { "type", ColorTranslator.FromHtml(TypeColor) },
        { "string", ColorTranslator.FromHtml(StringColor) },
        { "comment", ColorTranslator.FromHtml(CommentColor) },
        { "preprocessor", ColorTranslator.FromHtml(PreprocessorColor) },
        { "number", ColorTranslator.FromHtml(NumberColor) },
        { "error", ColorTranslator.FromHtml(ErrorColor) },
        { "normal", ColorTranslator.FromHtml(NormalColor) }
    };

    /// <summary>
    /// Creates a dark theme configuration
    /// </summary>
    public static SyntaxHighlightingConfig CreateDarkTheme()
    {
        return new SyntaxHighlightingConfig
        {
            ThemeName = "Dark",
            KeywordColor = "#569CD6",      // Light Blue
            TypeColor = "#4EC9B0",         // Teal
            StringColor = "#CE9178",       // Orange
            CommentColor = "#57A64A",      // Green
            PreprocessorColor = "#9B9B9B", // Light Gray
            NumberColor = "#B5CEA8",       // Light Green
            ErrorColor = "#FF4444",        // Bright Red
            NormalColor = "#D4D4D4"        // Light Gray
        };
    }

    /// <summary>
    /// Creates a Visual Studio theme configuration
    /// </summary>
    public static SyntaxHighlightingConfig CreateVSTheme()
    {
        return new SyntaxHighlightingConfig
        {
            ThemeName = "Visual Studio",
            KeywordColor = "#0000FF",      // Blue
            TypeColor = "#2B91AF",         // Steel Blue
            StringColor = "#A31515",       // Dark Red
            CommentColor = "#008000",      // Green
            PreprocessorColor = "#808080", // Gray
            NumberColor = "#000000",       // Black
            ErrorColor = "#FF0000",        // Red
            NormalColor = "#000000"        // Black
        };
    }

    /// <summary>
    /// Creates a high contrast theme for accessibility
    /// </summary>
    public static SyntaxHighlightingConfig CreateHighContrastTheme()
    {
        return new SyntaxHighlightingConfig
        {
            ThemeName = "High Contrast",
            KeywordColor = "#0000FF",      // Blue
            TypeColor = "#800080",         // Purple
            StringColor = "#008000",       // Green
            CommentColor = "#808080",      // Gray
            PreprocessorColor = "#FF0000", // Red
            NumberColor = "#000080",       // Navy
            ErrorColor = "#FFFF00",        // Yellow
            NormalColor = "#000000"        // Black
        };
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public bool IsValid()
    {
        try
        {
            // Try to convert all colors to ensure they are valid hex colors
            var colors = ColorScheme;
            return !string.IsNullOrWhiteSpace(ThemeName);
        }
        catch
        {
            return false;
        }
    }
}