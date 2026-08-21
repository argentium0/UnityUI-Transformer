using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UnityEngine;
using UnityUITransformer.App.Services;

namespace UnityUITransformer.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _isConnected;
        private bool _isConnecting;
        private string _connectedUser = string.Empty;
        private string _figmaUrl = string.Empty;
        private string _unityAssetsPath = string.Empty;
        private bool _isSyncing;
        private bool _isSyncComplete;
        private double _syncProgress;
        private string _syncStatusText = "Idle";

        public bool IsSyncComplete
        {
            get => _isSyncComplete;
            set => SetProperty(ref _isSyncComplete, value);
        }

        private bool _isTerminalExpanded = true;
        private bool _autoScrollEnabled = true;

        private ViewModelBase _currentView = null!;
        private int _currentStepIndex = 1;

        public AuthViewModel AuthVM { get; }
        public ConfigViewModel ConfigVM { get; }
        public SyncViewModel SyncVM { get; }

        public string VersionTag => "v1.0.0-pro-max";

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (SetProperty(ref _currentStepIndex, value))
                {
                    UpdateCurrentView();
                    OnPropertyChanged(nameof(IsStep1Active));
                    OnPropertyChanged(nameof(IsStep2Active));
                    OnPropertyChanged(nameof(IsStep3Active));
                }
            }
        }

        public bool IsStep1Active => CurrentStepIndex == 1;
        public bool IsStep2Active => CurrentStepIndex == 2;
        public bool IsStep3Active => CurrentStepIndex == 3;

        public bool CanNavigateToConfig => IsConnected;
        public bool CanNavigateToSync => IsConnected && IsStep2Completed;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    OnPropertyChanged(nameof(ConnectionStatusText));
                    OnPropertyChanged(nameof(IsStep1Completed));
                    OnPropertyChanged(nameof(IsStep2ActiveState));
                    OnPropertyChanged(nameof(IsStep3ActiveState));
                    OnPropertyChanged(nameof(CanNavigateToConfig));
                    OnPropertyChanged(nameof(CanNavigateToSync));
                    UpdateCanSync();
                }
            }
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                if (SetProperty(ref _isConnecting, value))
                {
                    OnPropertyChanged(nameof(ConnectionStatusText));
                }
            }
        }

        private string _avatarUrl = "pack://application:,,,/Assets/figma_avatar.png";

        public string AvatarUrl
        {
            get => _avatarUrl;
            set
            {
                if (SetProperty(ref _avatarUrl, value))
                {
                    UpdateAvatarSource(value);
                }
            }
        }

        private ImageSource? _userAvatarSource;
        public ImageSource? UserAvatarSource
        {
            get => _userAvatarSource;
            set => SetProperty(ref _userAvatarSource, value);
        }

        private void UpdateAvatarSource(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "pack://application:,,,/Assets/figma_avatar.png";
            }

            try
            {
                var kind = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                    ? UriKind.Absolute 
                    : UriKind.RelativeOrAbsolute;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url, kind);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();

                UserAvatarSource = bitmap;
            }
            catch
            {
                try
                {
                    var fallback = new BitmapImage();
                    fallback.BeginInit();
                    fallback.UriSource = new Uri("pack://application:,,,/Assets/figma_avatar.png", UriKind.Absolute);
                    fallback.CacheOption = BitmapCacheOption.OnLoad;
                    fallback.EndInit();
                    if (fallback.CanFreeze) fallback.Freeze();

                    UserAvatarSource = fallback;
                }
                catch { }
            }

            OnPropertyChanged(nameof(UserAvatarSource));
        }

        public string ConnectedUser
        {
            get => _connectedUser;
            set
            {
                if (SetProperty(ref _connectedUser, value))
                {
                    OnPropertyChanged(nameof(ConnectionStatusText));
                }
            }
        }

        public string ConnectionStatusText
        {
            get
            {
                if (IsConnecting) return "Connecting...";
                if (IsConnected) return "Connected";
                return "Disconnected";
            }
        }

        public string FigmaUrl
        {
            get => _figmaUrl;
            set
            {
                if (SetProperty(ref _figmaUrl, value))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (!value.Contains("figma.com/file/") && !value.Contains("figma.com/design/"))
                        {
                            ConfigValidationError = "Invalid Figma URL. Please ensure it contains a specific node-id.";
                        }
                        else if (!value.Contains("node-id=") && !value.Contains("%3A"))
                        {
                            ConfigValidationError = "Invalid Figma URL. A specific node-id is required.";
                        }
                        else
                        {
                            ConfigValidationError = string.Empty;
                        }
                    }
                    else
                    {
                        ConfigValidationError = string.Empty;
                    }

                    OnPropertyChanged(nameof(IsFigmaUrlValid));
                    OnPropertyChanged(nameof(IsStep2Completed));
                    OnPropertyChanged(nameof(IsStep3ActiveState));
                    OnPropertyChanged(nameof(CanNavigateToSync));
                    UpdateCanSync();
                }
            }
        }

        public string UnityAssetsPath
        {
            get => _unityAssetsPath;
            set
            {
                if (SetProperty(ref _unityAssetsPath, value))
                {
                    OnPropertyChanged(nameof(IsUnityPathValid));
                    OnPropertyChanged(nameof(IsStep2Completed));
                    OnPropertyChanged(nameof(IsStep3ActiveState));
                    OnPropertyChanged(nameof(CanNavigateToSync));
                    UpdateCanSync();
                }
            }
        }

        public bool IsFigmaUrlValid =>
            !string.IsNullOrWhiteSpace(FigmaUrl) &&
            (FigmaUrl.Contains("figma.com/file/") || FigmaUrl.Contains("figma.com/design/")) &&
            (FigmaUrl.Contains("node-id=") || FigmaUrl.Contains("%3A"));

        public bool IsUnityPathValid =>
            !string.IsNullOrWhiteSpace(UnityAssetsPath);

        // Step Progression State Flags
        public bool IsStep1Completed => IsConnected;
        public bool IsStep2ActiveState => IsConnected;
        public bool IsStep2Completed => IsFigmaUrlValid && IsUnityPathValid;
        public bool IsStep3ActiveState => IsConnected && IsStep2Completed;

        public bool CanSync => IsConnected && IsStep2Completed && !IsSyncing;

        public bool IsSyncing
        {
            get => _isSyncing;
            set
            {
                if (SetProperty(ref _isSyncing, value))
                {
                    UpdateCanSync();
                }
            }
        }

        public double SyncProgress
        {
            get => _syncProgress;
            set => SetProperty(ref _syncProgress, value);
        }

        public string SyncStatusText
        {
            get => _syncStatusText;
            set => SetProperty(ref _syncStatusText, value);
        }

        public bool IsTerminalExpanded
        {
            get => _isTerminalExpanded;
            set
            {
                if (SetProperty(ref _isTerminalExpanded, value))
                {
                    OnPropertyChanged(nameof(TerminalToggleText));
                }
            }
        }

        public bool AutoScrollEnabled
        {
            get => _autoScrollEnabled;
            set => SetProperty(ref _autoScrollEnabled, value);
        }

        public string TerminalToggleText => IsTerminalExpanded ? "Collapse ▲" : "Expand ▼";

        public ObservableCollection<LogEntryModel> LogEntries { get; } = new();

        public ICommand ConnectCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand ContinueToSyncCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand ToggleTerminalCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand NavigateToStepCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ToggleSettingsCommand { get; }
        public ICommand CloseSettingsCommand { get; }
        public ICommand DownloadManualCommand { get; }

        private bool _isSettingsOpen;
        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set
            {
                if (SetProperty(ref _isSettingsOpen, value) && value)
                {
                    SettingsStatusMessage = string.Empty;
                }
            }
        }

        private string _settingsStatusMessage = string.Empty;
        public string SettingsStatusMessage
        {
            get => _settingsStatusMessage;
            set
            {
                if (SetProperty(ref _settingsStatusMessage, value))
                {
                    OnPropertyChanged(nameof(HasSettingsStatusMessage));
                }
            }
        }

        public bool HasSettingsStatusMessage => !string.IsNullOrWhiteSpace(SettingsStatusMessage);

        private string _configValidationError = string.Empty;
        public string ConfigValidationError
        {
            get => _configValidationError;
            set
            {
                if (SetProperty(ref _configValidationError, value))
                {
                    OnPropertyChanged(nameof(HasConfigValidationError));
                    OnPropertyChanged(nameof(ErrorMessage));
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasConfigValidationError => !string.IsNullOrWhiteSpace(ConfigValidationError);
        public string ErrorMessage => ConfigValidationError;
        public bool HasError => HasConfigValidationError;

        private readonly SupabaseAuthService _supabaseAuthService;
        private readonly FigmaApiService _figmaApiService;
        private readonly UxmlGenerator _uxmlGenerator;
        private readonly UssGenerator _ussGenerator;
        private readonly ExportService _exportService;
        private readonly SecureStorageService _secureStorageService;
        private readonly FigmaLocalBridgeServer _localBridgeServer;

        public MainViewModel(
            SupabaseAuthService? supabaseAuthService = null,
            FigmaApiService? figmaApiService = null,
            UxmlGenerator? uxmlGenerator = null,
            UssGenerator? ussGenerator = null,
            ExportService? exportService = null,
            SecureStorageService? secureStorageService = null)
        {
            _supabaseAuthService = supabaseAuthService ?? new SupabaseAuthService();
            _figmaApiService = figmaApiService ?? new FigmaApiService(_supabaseAuthService);
            _uxmlGenerator = uxmlGenerator ?? new UxmlGenerator();
            _ussGenerator = ussGenerator ?? new UssGenerator();
            _exportService = exportService ?? new ExportService();
            _secureStorageService = secureStorageService ?? new SecureStorageService();

            _localBridgeServer = new FigmaLocalBridgeServer("http://127.0.0.1:5142/sync/");
            _localBridgeServer.PayloadReceivedAndProcessed += OnFigmaBridgePayloadReceived;
            _localBridgeServer.Start(() => UnityAssetsPath);

            AuthVM = new AuthViewModel(this);
            ConfigVM = new ConfigViewModel(this);
            SyncVM = new SyncViewModel(this);

            _currentView = AuthVM;
            UpdateAvatarSource(AvatarUrl);

            ConnectCommand = new RelayCommand(async _ => await ExecuteConnectAsync(), _ => !IsConnecting);
            BrowseFolderCommand = new RelayCommand(_ => ExecuteBrowseFolder(), _ => IsStep2ActiveState);
            ContinueToSyncCommand = new RelayCommand(async _ => await ExecuteContinueToSync(), _ => CanNavigateToSync);
            SyncCommand = new RelayCommand(async _ => await ExecuteSyncAsync(), _ => CanSync);
            ToggleTerminalCommand = new RelayCommand(() => IsTerminalExpanded = !IsTerminalExpanded);
            ClearLogsCommand = new RelayCommand(() => LogEntries.Clear());
            NavigateToStepCommand = new RelayCommand(param => ExecuteNavigateToStep(param), param => CanNavigateToStep(param));
            OpenFolderCommand = new RelayCommand(_ => ExecuteOpenFolder(), _ => !IsSyncing);
            ResetCommand = new RelayCommand(_ => ExecuteReset(), _ => !IsSyncing);
            DisconnectCommand = new RelayCommand(async _ => await ExecuteDisconnectAsync());
            ToggleSettingsCommand = new RelayCommand(() => IsSettingsOpen = !IsSettingsOpen);
            CloseSettingsCommand = new RelayCommand(() => IsSettingsOpen = false);
            DownloadManualCommand = new RelayCommand(_ => ExecuteDownloadManual());

            // Subscribe to ShimLogSink live events
            ShimLogSink.OnLog += OnShimLogReceived;

            // Check for saved DPAPI encrypted session token
            var savedToken = _secureStorageService.LoadSessionToken();
            if (!string.IsNullOrEmpty(savedToken))
            {
                IsConnected = true;
                ConnectedUser = "Figma Developer";
                AvatarUrl = "pack://application:,,,/Assets/figma_avatar.png";
                ShimLogSink.RaiseLog(ShimLogLevel.Info, "[DPAPI SECURE LOGIN] Restored encrypted session token from Windows ProtectedData.");
            }

            // Log initial startup event
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Figma → Unity UI Transformer (Pro Max v1.0.0) initialized.");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "[LOCAL BRIDGE] Listener active on http://127.0.0.1:5142/sync/");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Guided 3-Screen Architecture ready. Screen 1: Figma Authentication.");
        }

        private void OnFigmaBridgePayloadReceived(string fileName, string uxmlPath, string ussPath)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsSyncing = false;
                IsSyncComplete = true;
                SyncProgress = 100.0;
                SyncStatusText = $"Synced {fileName} via Local Bridge";

                ShimLogSink.RaiseLog(ShimLogLevel.Info, $"[LOCAL BRIDGE] Received payload from Figma Plugin for component '{fileName}'.");
                ShimLogSink.RaiseLog(ShimLogLevel.Info, $"[LOCAL BRIDGE] Output UXML: {uxmlPath}");
                ShimLogSink.RaiseLog(ShimLogLevel.Info, $"[LOCAL BRIDGE] Output USS: {ussPath}");
            });
        }

        private void UpdateCurrentView()
        {
            CurrentView = CurrentStepIndex switch
            {
                1 => AuthVM,
                2 => ConfigVM,
                3 => SyncVM,
                _ => AuthVM
            };
        }

        private bool CanNavigateToStep(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int step))
            {
                return step switch
                {
                    1 => true,
                    2 => IsConnected,
                    3 => IsConnected && IsStep2Completed,
                    _ => false
                };
            }
            return false;
        }

        private void ExecuteNavigateToStep(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int step))
            {
                if (CanNavigateToStep(parameter))
                {
                    CurrentStepIndex = step;
                }
            }
        }

        private async Task ExecuteConnectAsync()
        {
            if (IsConnecting || IsConnected) return;

            IsConnecting = true;
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Initiating Figma OAuth sign-in via Supabase C# SDK...");

            try
            {
                var result = await _supabaseAuthService.AuthenticateWithFigmaAsync();

                if (result.IsSuccess)
                {
                    ConnectedUser = result.UserName;
                    if (!string.IsNullOrEmpty(result.AvatarUrl))
                    {
                        AvatarUrl = result.AvatarUrl;
                    }
                    IsConnected = true;

                    if (!string.IsNullOrEmpty(result.AccessToken))
                    {
                        _secureStorageService.SaveSessionToken(result.AccessToken);

                        // Query Figma REST API /v1/me for user handle and profile image URL
                        var (handle, imgUrl, _) = await _figmaApiService.GetFigmaUserProfileAsync(result.AccessToken);
                        if (!string.IsNullOrEmpty(handle)) ConnectedUser = handle;
                        if (!string.IsNullOrEmpty(imgUrl)) AvatarUrl = imgUrl;
                    }

                    ShimLogSink.RaiseLog(ShimLogLevel.Info, "Figma OAuth session token successfully retrieved via Supabase.");
                    ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Authenticated as {ConnectedUser}. Auto-transitioning to Screen 2: Target Configuration.");

                    // Transition automatically to Screen 2 (ConfigView)
                    CurrentStepIndex = 2;
                }
                else
                {
                    ShimLogSink.RaiseLog(ShimLogLevel.Error, $"Figma OAuth authentication failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ShimLogSink.RaiseLog(ShimLogLevel.Error, $"Supabase OAuth exception: {ex.Message}");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        private void ExecuteBrowseFolder()
        {
            if (!IsStep2ActiveState) return;

            var dialog = new OpenFolderDialog
            {
                Title = "Select Unity Target Assets Folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                UnityAssetsPath = dialog.FolderName;
                ConfigValidationError = string.Empty;
                ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Selected Unity Target Directory: {UnityAssetsPath}");
            }
        }

        private Task ExecuteContinueToSync()
        {
            ConfigValidationError = string.Empty;

            // 1. URL Validation Guardrail
            if (string.IsNullOrWhiteSpace(FigmaUrl) || 
               (!FigmaUrl.Contains("figma.com/file/") && !FigmaUrl.Contains("figma.com/design/")))
            {
                ConfigValidationError = "Invalid Figma URL. Please ensure it contains a specific node-id.";
                return Task.CompletedTask;
            }

            if (!FigmaUrl.Contains("node-id=") && !FigmaUrl.Contains("%3A"))
            {
                ConfigValidationError = "Invalid Figma URL. A specific node-id is required.";
                return Task.CompletedTask;
            }

            // 2. Directory Validation Guardrail
            if (string.IsNullOrWhiteSpace(UnityAssetsPath) || !System.IO.Directory.Exists(UnityAssetsPath))
            {
                ConfigValidationError = "Please select a valid local Unity directory.";
                return Task.CompletedTask;
            }

            IsSyncComplete = false;
            CurrentStepIndex = 3;
            return Task.CompletedTask;
        }

        private void ExecuteOpenFolder()
        {
            try
            {
                string targetDir = string.IsNullOrWhiteSpace(UnityAssetsPath)
                    ? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "UI", "Generated")
                    : UnityAssetsPath;

                if (!System.IO.Directory.Exists(targetDir))
                {
                    System.IO.Directory.CreateDirectory(targetDir);
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = targetDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShimLogSink.RaiseLog(ShimLogLevel.Error, $"Failed to open folder: {ex.Message}");
            }
        }

        private void ExecuteReset()
        {
            IsSyncComplete = false;
            CurrentStepIndex = 2;
        }

        private async Task ExecuteDisconnectAsync()
        {
            await _supabaseAuthService.SignOutAsync();
            _secureStorageService.ClearSession();
            IsConnected = false;
            ConnectedUser = "Disconnected";
            _avatarUrl = "pack://application:,,,/Assets/figma_avatar.png";
            UserAvatarSource = null;
            OnPropertyChanged(nameof(AvatarUrl));
            OnPropertyChanged(nameof(UserAvatarSource));
            UpdateAvatarSource("pack://application:,,,/Assets/figma_avatar.png");
            CurrentStepIndex = 1;
            ConfigValidationError = string.Empty;
            IsSettingsOpen = false;
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "[SESSION DISCONNECTED] Purged DPAPI session.dat file and reset to Step 1 (AuthView).");
        }

        private void ExecuteDownloadManual()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Select Destination Folder for User Manual"
                };

                if (dialog.ShowDialog() == true)
                {
                    string folder = dialog.FolderName;
                    string pdfPath = System.IO.Path.Combine(folder, "UnityUI_Transformer_User_Manual.pdf");
                    string mdPath = System.IO.Path.Combine(folder, "UserManual.md");

                    // Generate Comprehensive PDF Manual
                    ManualPdfGenerator.GeneratePdfManual(pdfPath);

                    string manualContent = @"# UnityUI Transformer — User Manual & Setup Guide

Welcome to the **UnityUI Transformer**! This utility bridges Figma design frames directly into Unity UI Toolkit (`UXML` layout and `USS` style sheets).

---

## 1. Quick Start Guide

### Step 1: Figma Authentication
1. Launch UnityUI Transformer.
2. Click **Connect with Figma** to authenticate using your Figma account via OAuth.
3. Once authenticated, your session token is securely encrypted locally via **Windows DPAPI** (`ProtectedData`).

### Step 2: Configuration
1. Open your design file in Figma.
2. Select any **Frame** or **Component** you wish to export into Unity.
3. Copy the URL from your browser address bar (ensure it includes `node-id=...`).
4. Paste the URL into the **Figma Design File / Node URL** text box.
5. Browse and select your target Unity project directory (e.g. `C:\MyProject\Assets\UI`).

### Step 3: Transformation Pipeline
1. Click **Continue to Sync**.
2. Review your sync configuration and hit **Start Transformation**.
3. Live transformation logs will stream in the console terminal.
4. Upon completion, click **Open Target Folder** to view your generated `.uxml` and `.uss` assets in Unity!

---

## 2. Advanced Tips & Troubleshooting

- **Node ID Requirement:** Figma URLs must contain a specific `node-id=` parameter so the engine knows which frame to process.
- **Auto Layout to USS Flexbox:** Figma Auto-Layout properties (padding, gap, alignment, flex-direction) automatically map to Unity UI Toolkit USS flex attributes.
- **Session Management:** You can clear your stored credentials at any time from the app Settings menu using **Disconnect Account**.

---

*Generated by UnityUI Transformer (Pro Max v1.0.0)*
";

                    System.IO.File.WriteAllText(mdPath, manualContent);
                    SettingsStatusMessage = $"UnityUI_Transformer_User_Manual.pdf exported to {folder}";
                    ShimLogSink.RaiseLog(ShimLogLevel.Info, $"User manual exported to: {pdfPath}");
                }
            }
            catch (Exception ex)
            {
                SettingsStatusMessage = $"Export failed: {ex.Message}";
                ShimLogSink.RaiseLog(ShimLogLevel.Error, $"Failed to export user manual: {ex.Message}");
            }
        }

        private async Task ExecuteSyncAsync()
        {
            if (!CanSync) return;

            IsSyncing = true;
            IsSyncComplete = false;
            SyncProgress = 0;
            SyncStatusText = "Initializing Engine...";

            // Clear previous terminal clutter
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() => LogEntries.Clear());
            }
            else
            {
                lock (_logEntriesLock)
                {
                    LogEntries.Clear();
                }
            }

            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Starting Figma → Unity UI transformation...");

            try
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(250);
                    UpdateProgress(25, "Fetching Figma design nodes...");

                    var (fileId, nodeId) = FigmaApiService.ParseFigmaUrl(FigmaUrl);
                    ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Fetching Figma node {nodeId} (File ID: {fileId})...");

                    var rootNode = await _figmaApiService.GetFigmaNodeModelAsync(FigmaUrl);
                    string rootName = rootNode.Name ?? "MainScreen";

                    await Task.Delay(300);
                    UpdateProgress(55, "Generating UXML layout tree...");

                    string ussFileName = $"{UxmlGenerator.SanitizeName(rootName)}.uss";
                    string uxmlContent = _uxmlGenerator.GenerateUxml(rootNode, ussFileName);
                    ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Generated {UxmlGenerator.SanitizeName(rootName)}.uxml layout.");

                    await Task.Delay(300);
                    UpdateProgress(80, "Generating USS styles...");

                    string ussContent = _ussGenerator.GenerateUss(rootNode);
                    ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Generated {ussFileName} stylesheet.");

                    await Task.Delay(250);
                    UpdateProgress(95, "Saving output files...");

                    string uxmlPath = await _exportService.ExportUxmlAsync(uxmlContent, UnityAssetsPath, rootName);
                    string ussPath = await _exportService.ExportUssAsync(ussContent, UnityAssetsPath, rootName);

                    await Task.Delay(200);
                    UpdateProgress(100, "Transformation Complete!");

                    ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Saved {System.IO.Path.GetFileName(uxmlPath)} and {System.IO.Path.GetFileName(ussPath)} to {UnityAssetsPath}");
                    ShimLogSink.RaiseLog(ShimLogLevel.Info, "✓ Transformation finished successfully.");

                    if (Application.Current != null)
                    {
                        _ = Application.Current.Dispatcher.InvokeAsync(() => IsSyncComplete = true);
                    }
                    else
                    {
                        IsSyncComplete = true;
                    }
                });
            }
            catch (Exception ex)
            {
                ShimLogSink.RaiseLog(ShimLogLevel.Error, $"Transformation Error: {ex.Message}");
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void UpdateProgress(double progress, string statusText)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SyncProgress = progress;
                    SyncStatusText = statusText;
                });
            }
            else
            {
                SyncProgress = progress;
                SyncStatusText = statusText;
            }
        }

        private readonly object _logEntriesLock = new object();

        private void OnShimLogReceived(object? sender, ShimLogEventArgs e)
        {
            var entry = new LogEntryModel(e.Level, e.Message);
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_logEntriesLock)
                    {
                        LogEntries.Add(entry);
                    }
                });
            }
            else
            {
                lock (_logEntriesLock)
                {
                    LogEntries.Add(entry);
                }
            }
        }

        private void UpdateCanSync()
        {
            OnPropertyChanged(nameof(CanSync));
            (SyncCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (BrowseFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ContinueToSyncCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (NavigateToStepCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
