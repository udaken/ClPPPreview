using System.Drawing;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace ClPPPreview.Utilities;

/// <summary>
/// Provides syntax highlighting functionality for C/C++ code in RichTextBox
/// </summary>
public class CppSyntaxHighlighter : IDisposable
{
    // Windows API constants and methods for preventing flicker
    private const int WM_SETREDRAW = 0x000B;
    
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
    
    private void BeginUpdate()
    {
        SendMessage(_textBox.Handle, WM_SETREDRAW, false, 0);
    }
    
    private void EndUpdate()
    {
        SendMessage(_textBox.Handle, WM_SETREDRAW, true, 0);
        _textBox.Invalidate();
    }
    private readonly RichTextBox _textBox;
    private readonly Dictionary<string, Color> _colorScheme;
    private readonly string[] _keywords;
    private readonly string[] _types;
    private readonly string[] _preprocessorDirectives;
    private bool _disposed = false;
    private bool _isHighlighting = false;
    private readonly System.Windows.Forms.Timer _highlightingTimer;

    public CppSyntaxHighlighter(RichTextBox textBox)
    {
        _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
        
        // Create debounce timer for syntax highlighting (shorter delay than preprocessing)
        _highlightingTimer = new System.Windows.Forms.Timer
        {
            Interval = 150 // 150ms delay for responsive highlighting
        };
        _highlightingTimer.Tick += (s, e) =>
        {
            _highlightingTimer.Stop();
            PerformHighlighting();
        };
        
        // Default color scheme
        _colorScheme = new Dictionary<string, Color>
        {
            { "keyword", Color.Blue },
            { "type", Color.DarkCyan },
            { "string", Color.Brown },
            { "comment", Color.Green },
            { "preprocessor", Color.Gray },
            { "number", Color.DarkMagenta },
            { "error", Color.Red },
            { "normal", Color.Black }
        };

        // C++ keywords
        _keywords = new[]
        {
            "auto", "break", "case", "char", "const", "continue", "default", "do",
            "double", "else", "enum", "extern", "float", "for", "goto", "if",
            "int", "long", "register", "return", "short", "signed", "sizeof", "static",
            "struct", "switch", "typedef", "union", "unsigned", "void", "volatile", "while",
            "class", "private", "public", "protected", "virtual", "friend", "inline",
            "operator", "overload", "template", "this", "new", "delete", "try", "catch",
            "throw", "const_cast", "dynamic_cast", "reinterpret_cast", "static_cast",
            "typeid", "typename", "using", "namespace", "mutable", "explicit",
            "bool", "true", "false", "and", "or", "not", "xor", "bitand", "bitor",
            "compl", "and_eq", "or_eq", "xor_eq", "not_eq", "constexpr", "decltype",
            "nullptr", "alignas", "alignof", "thread_local", "noexcept"
        };

        // Common types
        _types = new[]
        {
            "std", "string", "vector", "map", "set", "list", "queue", "stack",
            "pair", "shared_ptr", "unique_ptr", "weak_ptr", "function",
            "size_t", "ptrdiff_t", "wchar_t", "char16_t", "char32_t",
            "int8_t", "int16_t", "int32_t", "int64_t",
            "uint8_t", "uint16_t", "uint32_t", "uint64_t"
        };

        // Preprocessor directives
        _preprocessorDirectives = new[]
        {
            "#include", "#define", "#undef", "#ifdef", "#ifndef", "#if", "#else",
            "#elif", "#endif", "#error", "#warning", "#pragma", "#line"
        };
    }

    /// <summary>
    /// Applies syntax highlighting to the entire text
    /// </summary>
    /// <summary>
    /// Applies syntax highlighting to the entire text
    /// </summary>
    /// <summary>
    /// Triggers debounced syntax highlighting
    /// </summary>
    public void HighlightSyntax()
    {
        if (_disposed)
            return;

        // Restart the debounce timer
        _highlightingTimer.Stop();
        _highlightingTimer.Start();
    }

    /// <summary>
    /// Applies syntax highlighting immediately without debouncing
    /// </summary>
    public void HighlightSyntaxImmediate()
    {
        PerformHighlighting();
    }

    /// <summary>
    /// Performs the actual syntax highlighting
    /// </summary>
    private void PerformHighlighting()
    {
        if (_disposed || _isHighlighting)
            return;

        try
        {
            _isHighlighting = true;
            
            // Temporarily disable redrawing to prevent flicker
            BeginUpdate();
            
            var originalSelection = _textBox.SelectionStart;
            var originalLength = _textBox.SelectionLength;

            // Clear existing formatting efficiently
            _textBox.SelectAll();
            _textBox.SelectionColor = _colorScheme["normal"];
            _textBox.SelectionFont = new Font(_textBox.Font, FontStyle.Regular);

            var text = _textBox.Text;
            
            // Apply syntax highlighting in order of priority
            HighlightComments(text);      // First, so comments override other highlighting
            HighlightStrings(text);       // Second, so strings override keywords
            HighlightPreprocessor(text);  // Third, for preprocessor directives
            HighlightKeywords(text);      // Fourth, for keywords
            HighlightTypes(text);         // Fifth, for types
            HighlightNumbers(text);       // Last, for numbers

            // Restore original selection
            _textBox.SelectionStart = Math.Min(originalSelection, _textBox.Text.Length);
            _textBox.SelectionLength = Math.Min(originalLength, _textBox.Text.Length - _textBox.SelectionStart);
            
            // Re-enable redrawing
            EndUpdate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Syntax highlighting error: {ex.Message}");
            // Ensure EndUpdate is called even if exception occurs
            try { EndUpdate(); } catch { }
        }
        finally
        {
            _isHighlighting = false;
        }
    }

    /// <summary>
    /// Highlights error lines with specified line numbers
    /// </summary>
    public void HighlightErrors(IEnumerable<int> errorLines)
    {
        if (_disposed || errorLines == null)
            return;

        try
        {
            var lines = _textBox.Lines;
            foreach (var lineNumber in errorLines)
            {
                if (lineNumber > 0 && lineNumber <= lines.Length)
                {
                    var lineIndex = lineNumber - 1;
                    var startIndex = GetLineStartIndex(lineIndex);
                    var lineLength = lines[lineIndex].Length;
                    
                    if (startIndex >= 0 && lineLength > 0)
                    {
                        _textBox.Select(startIndex, lineLength);
                        _textBox.SelectionBackColor = Color.LightPink;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error highlighting error: {ex.Message}");
        }
    }

    private void HighlightKeywords(string text)
    {
        foreach (var keyword in _keywords)
        {
            HighlightPattern($@"\b{Regex.Escape(keyword)}\b", _colorScheme["keyword"], FontStyle.Bold);
        }
    }

    private void HighlightTypes(string text)
    {
        foreach (var type in _types)
        {
            HighlightPattern($@"\b{Regex.Escape(type)}\b", _colorScheme["type"], FontStyle.Regular);
        }
    }

    private void HighlightStrings(string text)
    {
        // String literals
        HighlightPattern(@"""[^""\\]*(?:\\.[^""\\]*)*""", _colorScheme["string"], FontStyle.Regular);
        // Character literals
        HighlightPattern(@"'[^'\\]*(?:\\.[^'\\]*)*'", _colorScheme["string"], FontStyle.Regular);
    }

    private void HighlightComments(string text)
    {
        // Single-line comments
        HighlightPattern(@"//.*$", _colorScheme["comment"], FontStyle.Italic, RegexOptions.Multiline);
        // Multi-line comments
        HighlightPattern(@"/\*.*?\*/", _colorScheme["comment"], FontStyle.Italic, RegexOptions.Singleline);
    }

    private void HighlightPreprocessor(string text)
    {
        foreach (var directive in _preprocessorDirectives)
        {
            HighlightPattern($@"^{Regex.Escape(directive)}\b.*$", _colorScheme["preprocessor"], FontStyle.Regular, RegexOptions.Multiline);
        }
    }

    private void HighlightNumbers(string text)
    {
        // Integer and floating-point numbers
        HighlightPattern(@"\b\d+\.?\d*f?\b", _colorScheme["number"], FontStyle.Regular);
        // Hexadecimal numbers
        HighlightPattern(@"\b0[xX][0-9a-fA-F]+\b", _colorScheme["number"], FontStyle.Regular);
    }

    private void HighlightPattern(string pattern, Color color, FontStyle fontStyle, RegexOptions options = RegexOptions.IgnoreCase)
    {
        try
        {
            var matches = Regex.Matches(_textBox.Text, pattern, options);
            foreach (Match match in matches)
            {
                _textBox.Select(match.Index, match.Length);
                _textBox.SelectionColor = color;
                _textBox.SelectionFont = new Font(_textBox.Font, fontStyle);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pattern highlighting error: {ex.Message}");
        }
    }

    private int GetLineStartIndex(int lineIndex)
    {
        try
        {
            if (lineIndex == 0) return 0;
            
            var lines = _textBox.Lines;
            var startIndex = 0;
            
            for (int i = 0; i < lineIndex && i < lines.Length; i++)
            {
                startIndex += lines[i].Length + Environment.NewLine.Length;
            }
            
            return startIndex;
        }
        catch
        {
            return 0;
        }
    }

    private Point GetScrollPos()
    {
        try
        {
            return new Point(_textBox.SelectionStart, _textBox.SelectionStart);
        }
        catch
        {
            return Point.Empty;
        }
    }

    private void SetScrollPos(Point position)
    {
        try
        {
            _textBox.SelectionStart = position.X;
            _textBox.ScrollToCaret();
        }
        catch
        {
            // Ignore scroll position restoration errors
        }
    }

    /// <summary>
    /// Updates the color scheme
    /// </summary>
    public void UpdateColorScheme(Dictionary<string, Color> newColorScheme)
    {
        if (newColorScheme != null)
        {
            foreach (var kvp in newColorScheme)
            {
                _colorScheme[kvp.Key] = kvp.Value;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _highlightingTimer?.Stop();
            _highlightingTimer?.Dispose();
            _disposed = true;
        }
    }
}