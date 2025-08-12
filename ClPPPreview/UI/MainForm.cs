using ClPPPreview.Models;
using ClPPPreview.Services;
using ClPPPreview.Utilities;

namespace ClPPPreview.UI;

public partial class MainForm : Form
{
    private readonly PreprocessorService _preprocessorService;
    private readonly ConfigManager _configManager;
    private readonly DebounceTimer _debounceTimer;
    private PreprocessorConfig _config;
    private CancellationTokenSource? _preprocessingCancellation;
    private bool _isProcessing = false;

    public MainForm()
    {
        InitializeComponent();
        
        // Initialize services
        _configManager = new ConfigManager();
        _preprocessorService = new PreprocessorService();
        
        // Load configuration
        _config = _configManager.LoadConfig();
        
        // Initialize debounce timer
        _debounceTimer = new DebounceTimer(OnDebounceTimerElapsed, _config.DebounceDelayMs);
        
        // Initialize UI
        InitializeUI();
        
        // Wire up event handlers
        WireUpEventHandlers();
        
        // Load initial configuration into UI
        LoadConfigurationToUI();
    }

    private void InitializeUI()
    {
        // Set window title
        this.Text = "MSVC Preprocessor Preview";
        
        // Set initial window size from configuration
        this.Size = new Size(_config.WindowWidth, _config.WindowHeight);
        
        // Set splitter distance
        if (splitContainer1.Width > _config.SplitterDistance)
            splitContainer1.SplitterDistance = _config.SplitterDistance;
        
        // Configure text boxes
        textBoxSourceCode.Font = new Font("Consolas", 10F, FontStyle.Regular);
        textBoxOutput.Font = new Font("Consolas", 9F, FontStyle.Regular);
        textBoxOutput.BackColor = SystemColors.Control;
        
        // Enable drag and drop for source code text box
        textBoxSourceCode.AllowDrop = true;
        
        // Set initial source code if empty
        if (string.IsNullOrWhiteSpace(textBoxSourceCode.Text))
        {
            textBoxSourceCode.Text = "#include <iostream>\r\n#include <cstdlib>\r\n\r\nint main()\r\n{\r\n    std::cout << \"Hello, World!\" << std::endl;\r\n    return 0;\r\n}";
        }
        
        // Initialize help button if it exists
        if (buttonHelp != null)
        {
            buttonHelp.Text = "cl.exe Help";
        }
        
        // Set initial status
        UpdateStatus("Ready");
    }

    private void WireUpEventHandlers()
    {
        // Text change events with debouncing
        textBoxSourceCode.TextChanged += OnSourceCodeChanged;
        textBoxBuildToolPath.TextChanged += OnBuildToolPathChanged;
        textBoxCommandLine.TextChanged += OnCommandLineChanged;
        
        // Drag and drop events for source code text box
        textBoxSourceCode.DragEnter += OnSourceCodeDragEnter;
        textBoxSourceCode.DragDrop += OnSourceCodeDragDrop;
        
        // Button events
        button1.Click += OnBrowseButtonClick;
        
        // Help button event (if buttonHelp exists)
        if (buttonHelp != null)
        {
            buttonHelp.Click += OnHelpButtonClick;
        }
        
        // Form events
        this.FormClosing += OnFormClosing;
        this.ResizeEnd += OnFormResizeEnd;
        
        // Splitter events
        splitContainer1.SplitterMoved += OnSplitterMoved;
    }

    private void LoadConfigurationToUI()
    {
        textBoxBuildToolPath.Text = _config.BuildToolPath;
        textBoxCommandLine.Text = _config.CommandLineArgs;
        
        // Load VsDevCmd path if textBoxVsDevCmdPath exists
        if (textBoxVsDevCmdPath != null)
        {
            textBoxVsDevCmdPath.Text = _config.VsDevCmdPath;
        }
        
        // Validate build tool path on startup
        ValidateBuildToolPath();
    }

    private void OnSourceCodeChanged(object? sender, EventArgs e)
    {
        if (_isProcessing)
            return;

        // Trigger debounced preprocessing
        _debounceTimer.Trigger();
    }

    private void OnBuildToolPathChanged(object? sender, EventArgs e)
    {
        _config.BuildToolPath = textBoxBuildToolPath.Text;
        ValidateBuildToolPath();
        
        // Trigger preprocessing if path is valid
        if (_preprocessorService.ValidateBuildToolPath(_config.BuildToolPath))
        {
            _debounceTimer.Trigger();
        }
    }

    private void OnCommandLineChanged(object? sender, EventArgs e)
    {
        _config.CommandLineArgs = textBoxCommandLine.Text;
        _debounceTimer.Trigger();
    }

    private void OnBrowseButtonClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select Visual Studio installation folder",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            // Try to find cl.exe in the selected directory
            var clPath = FindClExeInDirectory(dialog.SelectedPath);
            if (!string.IsNullOrEmpty(clPath))
            {
                textBoxBuildToolPath.Text = clPath;
            }
            else
            {
                MessageBox.Show(
                    "Could not find cl.exe in the selected directory.\n\n" +
                    "Please select a Visual Studio installation directory that contains:\n" +
                    "VC\\Tools\\MSVC\\{version}\\bin\\Hostx64\\x64\\cl.exe",
                    "cl.exe Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }

    private async void OnHelpButtonClick(object? sender, EventArgs e)
    {
        try
        {
            UpdateStatus("Getting cl.exe help...");
            
            var result = await _preprocessorService.GetCompilerHelpAsync(_config);
            
            if (result.Success)
            {
                textBoxOutput.Text = result.Output;
                UpdateStatus($"Help retrieved successfully ({result.Duration.TotalMilliseconds:F0}ms)");
            }
            else
            {
                var errorMessage = result.GetErrorMessage();
                textBoxOutput.Text = $"// Failed to get cl.exe help\r\n// {errorMessage}\r\n\r\n{result.ErrorOutput}";
                UpdateStatus("Help retrieval failed");
            }
        }
        catch (Exception ex)
        {
            textBoxOutput.Text = $"// Error getting cl.exe help\r\n// {ex.Message}";
            UpdateStatus("Help error occurred");
            System.Diagnostics.Debug.WriteLine($"Help button error: {ex}");
        }
    }

    private void OnShowIncludePathsClick(object? sender, EventArgs e)
    {
        try
        {
            var includePathInfo = _preprocessorService.GetIncludePathInfo(_config);
            textBoxOutput.Text = includePathInfo;
            UpdateStatus("Include path information displayed");
        }
        catch (Exception ex)
        {
            textBoxOutput.Text = $"// Error getting include path info\r\n// {ex.Message}";
            UpdateStatus("Include path error occurred");
            System.Diagnostics.Debug.WriteLine($"Include path info error: {ex}");
        }
    }

    private void OnSourceCodeDragEnter(object? sender, DragEventArgs e)
    {
        // Check if the dragged data contains file(s)
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
            {
                var file = files[0];
                var extension = Path.GetExtension(file).ToLowerInvariant();
                
                // Accept common C/C++ file extensions
                if (extension is ".c" or ".cpp" or ".cxx" or ".cc" or ".h" or ".hpp" or ".hxx" or ".txt")
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
        }
        
        e.Effect = DragDropEffects.None;
    }

    private void OnSourceCodeDragDrop(object? sender, DragEventArgs e)
    {
        try
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    var filePath = files[0];
                    
                    // Read file content
                    var content = File.ReadAllText(filePath);
                    textBoxSourceCode.Text = content;
                    
                    UpdateStatus($"Loaded file: {Path.GetFileName(filePath)}");
                }
            }
        }
        catch (Exception ex)
        {
            UpdateStatus("Error loading file");
            MessageBox.Show(
                $"Failed to load file:\n{ex.Message}",
                "File Load Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            
            System.Diagnostics.Debug.WriteLine($"Drag drop error: {ex}");
        }
    }

    private async void OnDebounceTimerElapsed()
    {
        // This runs on a background thread, so marshal to UI thread
        if (InvokeRequired)
        {
            Invoke(new Action(async () => await ProcessSourceCodeAsync()));
        }
        else
        {
            await ProcessSourceCodeAsync();
        }
    }

    private async Task ProcessSourceCodeAsync()
    {
        if (_isProcessing)
            return;

        var sourceCode = textBoxSourceCode.Text;
        
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            textBoxOutput.Text = "// No source code to process";
            UpdateStatus("Ready");
            return;
        }

        if (!_preprocessorService.ValidateBuildToolPath(_config.BuildToolPath))
        {
            textBoxOutput.Text = "// Invalid or missing cl.exe path\r\n// Please configure the build tool path";
            UpdateStatus("Configuration Error");
            return;
        }

        try
        {
            _isProcessing = true;
            UpdateStatus("Processing...");
            
            // Cancel any existing preprocessing
            _preprocessingCancellation?.Cancel();
            _preprocessingCancellation = new CancellationTokenSource();

            var result = await _preprocessorService.ProcessSourceAsync(
                sourceCode, 
                _config, 
                _preprocessingCancellation.Token);

            if (_preprocessingCancellation.Token.IsCancellationRequested)
                return;

            if (result.Success)
            {
                textBoxOutput.Text = result.Output;
                UpdateStatus($"Completed successfully ({result.Duration.TotalMilliseconds:F0}ms)");
            }
            else
            {
                var errorMessage = result.GetErrorMessage();
                textBoxOutput.Text = $"// Preprocessing failed\r\n// {errorMessage}\r\n\r\n{result.ErrorOutput}";
                UpdateStatus("Processing failed");
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled or new request came in
            UpdateStatus("Cancelled");
        }
        catch (Exception ex)
        {
            textBoxOutput.Text = $"// Unexpected error occurred\r\n// {ex.Message}";
            UpdateStatus("Error occurred");
            System.Diagnostics.Debug.WriteLine($"Processing error: {ex}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void ValidateBuildToolPath()
    {
        var path = textBoxBuildToolPath.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            textBoxBuildToolPath.BackColor = SystemColors.Window;
            return;
        }

        if (_preprocessorService.ValidateBuildToolPath(path))
        {
            textBoxBuildToolPath.BackColor = Color.LightGreen;
        }
        else
        {
            textBoxBuildToolPath.BackColor = Color.LightPink;
        }
    }

    private void UpdateStatus(string message)
    {
        // For now, we'll update the form title with status
        // In Task 3.3 we'll add a proper status bar
        this.Text = $"MSVC Preprocessor Preview - {message}";
    }

    private string FindClExeInDirectory(string directory)
    {
        try
        {
            // Look for cl.exe in common subdirectory patterns
            var searchPatterns = new[]
            {
                "VC\\Tools\\MSVC\\*\\bin\\Hostx64\\x64\\cl.exe",
                "VC\\Tools\\MSVC\\*\\bin\\Hostx86\\x86\\cl.exe",
                "VC\\bin\\cl.exe",
                "bin\\cl.exe"
            };

            foreach (var pattern in searchPatterns)
            {
                var searchPath = Path.Combine(directory, pattern);
                var matches = Directory.GetFiles(
                    Path.GetDirectoryName(searchPath)!, 
                    Path.GetFileName(searchPath), 
                    SearchOption.AllDirectories);

                if (matches.Length > 0)
                {
                    return matches.OrderByDescending(f => new FileInfo(f).LastWriteTime).First();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching for cl.exe: {ex.Message}");
        }

        return string.Empty;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // Save configuration before closing
        try
        {
            _config.WindowWidth = this.Width;
            _config.WindowHeight = this.Height;
            _config.SplitterDistance = splitContainer1.SplitterDistance;
            _configManager.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
        }

        // Cancel any ongoing processing
        _preprocessingCancellation?.Cancel();

        // Dispose of services
        _debounceTimer?.Dispose();
        _preprocessorService?.Dispose();
    }

    private void OnFormResizeEnd(object? sender, EventArgs e)
    {
        // Update configuration when user finishes resizing
        _config.WindowWidth = this.Width;
        _config.WindowHeight = this.Height;
    }

    private void OnSplitterMoved(object? sender, SplitterEventArgs e)
    {
        // Update configuration when splitter is moved
        _config.SplitterDistance = splitContainer1.SplitterDistance;
    }
}
