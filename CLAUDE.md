# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Visual Studio C++ の cl.exe(a.k.a. msvc) のプリプロセッサの結果をリアルタイムに確認するアプリケーションです。

## Development Commands

### Build Commands
- **Build solution**: `dotnet build ClppPreview.sln`
- **Build specific project**: `dotnet build ClppPreview/ClppPreview.csproj`
- **Clean build artifacts**: `dotnet clean ClppPreview.sln`

### Run Commands
- **Run application**: `dotnet run --project ClppPreview/ClppPreview.csproj`
- **Run with specific configuration**: `dotnet run --project ClppPreview/ClppPreview.csproj -c Release`

### Project Management
- **Add new project to solution**: `dotnet sln ClppPreview.sln add <new-project-path>`
- **Create new project**: `dotnet new winforms -n <project-name>`

# ClppPreview - Development Tasks

## Phase 1: Foundation Setup (Priority: High)

### Key Features **IMORTANT**
- [ ] cl.exe へ入力ファイルは、テキストボックス textBoxSourceCode の値を使うようにしてください
- [ ] cl.exe の出力は、textBoxOutput に表示してください
- [ ] cl.exe のコマンドラインには、一般的なWindows 環境向けのオプションを設定してください
- [ ] ビルドに必要な環境変数が設定されるように VsDevCmd.bat を実行してからcl.exeを実行してください
- [ ] Show Help of cl.exe 
- [ ] ファイルをドラッグアンドドロップして、textBoxSourceCode にファイルの内容を表示できるようにしてください

### Task 1.1: Project Structure Enhancement
- **Objective**: Organize code into proper architectural layers
- **Actions**:
  - Create `Models/` folder with data models
  - Create `Services/` folder for business logic
  - Create `UI/` folder and move Form1 files
  - Create `Utilities/` folder for helper classes
- **Acceptance Criteria**:
  - All files organized according to design document structure
  - Solution builds successfully after reorganization
  - No broken references or compilation errors

### Task 1.2: Data Models Implementation
- **Objective**: Create core data models for configuration and results
- **Files to Create**:
  - `Models/PreprocessorConfig.cs`
  - `Models/PreprocessResult.cs`
  - `Models/ProcessResult.cs`
- **Acceptance Criteria**:
  - Models include all properties defined in design document
  - Proper validation attributes where applicable
  - XML documentation for all public members

### Task 1.3: Configuration Manager
- **Objective**: Implement settings persistence and MSVC tool detection
- **Files to Create**:
  - `Services/ConfigManager.cs`
- **Implementation Details**:
  - JSON-based configuration storage in `%APPDATA%\ClppPreview\`
  - Automatic MSVC installation detection using registry and common paths
  - Default configuration management
- **Acceptance Criteria**:
  - Settings persist between application sessions
  - Automatic detection finds MSVC when installed
  - Graceful handling of missing or corrupted config files

## Phase 2: Core Infrastructure (Priority: High)

### Task 2.1: Process Executor Service
- **Objective**: Safe and efficient external process execution
- **Files to Create**:
  - `Services/ProcessExecutor.cs`
- **Implementation Details**:
  - Async process execution with cancellation support
  - Proper resource disposal and cleanup
  - Timeout handling and error capture
  - Security considerations for process invocation
- **Acceptance Criteria**:
  - Can execute cl.exe with custom arguments
  - Captures both stdout and stderr
  - Handles process cancellation gracefully
  - No resource leaks during repeated executions

### Task 2.2: File Manager Service
- **Objective**: Handle temporary file operations securely
- **Files to Create**:
  - `Services/FileManager.cs`
- **Implementation Details**:
  - Temporary file creation in secure location
  - Automatic cleanup on application exit
  - Path validation and security checks
- **Acceptance Criteria**:
  - Creates temporary files with appropriate permissions
  - Cleans up files automatically
  - Validates file paths for security
  - Handles file system permission issues

### Task 2.3: Preprocessor Service
- **Objective**: Main business logic for C++ preprocessing
- **Files to Create**:
  - `Services/PreprocessorService.cs`
- **Implementation Details**:
  - Integration with ProcessExecutor and FileManager
  - Command line construction for cl.exe
  - Result parsing and error handling
  - Configuration validation
- **Acceptance Criteria**:
  - Successfully preprocesses valid C++ code
  - Returns meaningful error messages for invalid code
  - Handles missing or invalid tool paths
  - Validates configuration before processing

## Phase 3: User Interface Enhancement (Priority: Medium)

### Task 3.1: UI Event Handling
- **Objective**: Implement proper event handling for all UI controls
- **Files to Modify**:
  - `UI/Form1.cs`
- **Implementation Details**:
  - Wire up all button click events
  - Implement text change handlers with debouncing
  - Add proper validation feedback
  - Implement async UI updates
- **Acceptance Criteria**:
  - Browse button opens folder dialog and updates path
  - Source code changes trigger preprocessing after delay
  - UI remains responsive during preprocessing
  - Error states clearly displayed to user

### Task 3.2: Debounced Text Processing
- **Objective**: Implement efficient real-time preprocessing
- **Files to Create**:
  - `Utilities/DebounceTimer.cs`
- **Files to Modify**:
  - `UI/Form1.cs`
- **Implementation Details**:
  - 500ms debounce delay after text changes
  - Cancellation of in-flight operations
  - Progress indication during processing
- **Acceptance Criteria**:
  - Preprocessing only triggered after user stops typing
  - Multiple rapid changes don't cause performance issues
  - Users see visual feedback during processing

### Task 3.3: Status and Error Display
- **Objective**: Improve user feedback and error communication
- **Files to Modify**:
  - `UI/Form1.Designer.cs`
  - `UI/Form1.cs`
- **Implementation Details**:
  - Add status bar for operation feedback
  - Color-coded output for different message types
  - Clear error message display
  - Progress indicators for long operations
- **Acceptance Criteria**:
  - Users always know the current application state
  - Errors are clearly communicated with actionable information
  - Success operations provide appropriate feedback

## Phase 4: Integration and Polish (Priority: Medium)

### Task 4.1: End-to-End Integration
- **Objective**: Connect all components for complete functionality
- **Files to Modify**:
  - `UI/Form1.cs`
  - All service classes as needed
- **Implementation Details**:
  - Wire up all services to UI layer
  - Implement proper dependency injection pattern
  - Add comprehensive error handling
- **Acceptance Criteria**:
  - Complete user workflow works from UI to preprocessor output
  - All error conditions handled gracefully
  - Performance meets requirements (responsive UI)

### Task 4.2: Path Validation and Security
- **Objective**: Ensure secure and robust path handling
- **Files to Create**:
  - `Utilities/PathValidator.cs`
- **Implementation Details**:
  - Executable path validation
  - Directory traversal prevention
  - Permission checking
  - Path normalization
- **Acceptance Criteria**:
  - Only valid executable paths accepted
  - No security vulnerabilities in path handling
  - Clear feedback for invalid paths

### Task 4.3: Application Settings Persistence
- **Objective**: Remember user preferences between sessions
- **Files to Modify**:
  - `UI/Form1.cs`
  - `Services/ConfigManager.cs`
- **Implementation Details**:
  - Window size and position persistence
  - Splitter position saving
  - Tool path and command line persistence
  - Last used source code (optional)
- **Acceptance Criteria**:
  - Application opens with previous configuration
  - Window layout preserved between sessions
  - User doesn't need to reconfigure after restart

## Phase 5: Testing and Validation (Priority: High)

### Task 5.1: Unit Testing Framework Setup
- **Objective**: Establish testing infrastructure
- **Actions**:
  - Create test project in solution
  - Add NUnit package references
  - Set up test project structure
- **Files to Create**:
  - `ClppPreview.Tests/ClppPreview.Tests.csproj`
  - `ClppPreview.Tests/ServiceTests/`
  - `ClppPreview.Tests/UtilityTests/`

### Task 5.2: Service Layer Testing
- **Objective**: Ensure business logic correctness
- **Test Files to Create**:
  - `ServiceTests/PreprocessorServiceTests.cs`
  - `ServiceTests/ConfigManagerTests.cs`
  - `ServiceTests/ProcessExecutorTests.cs`
- **Test Coverage**:
  - Happy path scenarios
  - Error conditions and edge cases
  - Configuration validation
  - Process execution with various inputs

### Task 5.3: Integration Testing
- **Objective**: Validate end-to-end functionality
- **Test Files to Create**:
  - `IntegrationTests/PreprocessingWorkflowTests.cs`
- **Test Scenarios**:
  - Complete preprocessing workflow
  - Configuration loading and saving
  - Error handling across components
  - Performance under load

## Phase 6: Documentation and Deployment (Priority: Low)

### Task 6.1: Code Documentation
- **Objective**: Add comprehensive XML documentation
- **Actions**:
  - Document all public APIs
  - Add usage examples in comments
  - Document configuration options
- **Acceptance Criteria**:
  - All public methods and properties documented
  - IntelliSense provides helpful information
  - Code self-documents its purpose and usage

### Task 6.2: User Documentation
- **Objective**: Create user-facing documentation
- **Files to Create**:
  - `README.md` (user guide)
  - `INSTALL.md` (installation instructions)
- **Content**:
  - Application overview and features
  - Installation and setup instructions
  - Usage examples and screenshots
  - Troubleshooting guide

### Task 6.3: Build and Packaging
- **Objective**: Prepare application for distribution
- **Actions**:
  - Configure release build settings
  - Create installer or packaging scripts
  - Set up version information
  - Create deployment documentation

## Risk Mitigation

### Technical Risks
1. **MSVC Tool Detection**: May fail on non-standard installations
   - **Mitigation**: Provide manual path selection, multiple detection strategies
2. **Process Execution Security**: Potential for code injection
   - **Mitigation**: Input sanitization, restricted process permissions
3. **Performance**: UI freezing during long preprocessing
   - **Mitigation**: Async operations, cancellation support, progress feedback

## Definition of Done

Each task is considered complete when:
- [ ] Code implementation matches design specifications
- [ ] All acceptance criteria met
- [ ] Unit tests written and passing (where applicable)
- [ ] Code reviewed and approved
- [ ] Documentation updated
- [ ] Integration testing passed
- [ ] No performance regressions introduced

