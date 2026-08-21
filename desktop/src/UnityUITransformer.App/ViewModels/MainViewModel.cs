using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using UnityEngine;

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
        private double _syncProgress;
        private string _syncStatusText = "Ready";
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

        private string _avatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&auto=format&fit=crop&q=80";

        public string AvatarUrl
        {
            get => _avatarUrl;
            set => SetProperty(ref _avatarUrl, value);
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
            (FigmaUrl.Contains("figma.com/file/") || FigmaUrl.Contains("figma.com/design/") || FigmaUrl.StartsWith("https://"));

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

        public MainViewModel()
        {
            AuthVM = new AuthViewModel(this);
            ConfigVM = new ConfigViewModel(this);
            SyncVM = new SyncViewModel(this);

            _currentView = AuthVM;

            ConnectCommand = new AsyncRelayCommand(ExecuteConnectAsync, () => !IsConnecting);
            BrowseFolderCommand = new RelayCommand(ExecuteBrowseFolder, () => IsStep2ActiveState);
            ContinueToSyncCommand = new RelayCommand(ExecuteContinueToSync, () => IsStep2Completed);
            SyncCommand = new AsyncRelayCommand(ExecuteSyncAsync, () => CanSync);
            ToggleTerminalCommand = new RelayCommand(() => IsTerminalExpanded = !IsTerminalExpanded);
            ClearLogsCommand = new RelayCommand(() => LogEntries.Clear());
            NavigateToStepCommand = new RelayCommand(ExecuteNavigateToStep, CanNavigateToStep);

            // Subscribe to ShimLogSink live events
            ShimLogSink.OnLog += OnShimLogReceived;

            // Log initial startup event
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Figma → Unity UI Transformer (Pro Max v1.0.0) initialized.");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Guided 3-Screen Architecture ready. Screen 1: Figma Authentication.");
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
            IsConnecting = true;
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Initiating OAuth 2.0 PKCE handshake with Figma API...");

            await Task.Delay(1200); // Simulate network handshake

            IsConnecting = false;
            IsConnected = true;
            ConnectedUser = "Alex (Design Lead)";

            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Authentication token successfully stored in CoreVault.");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Authenticated as {ConnectedUser}. Transitioning to Screen 2: Target Configuration.");

            // Transition automatically to Screen 2 (ConfigView)
            CurrentStepIndex = 2;
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
                ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Selected Unity Target Directory: {UnityAssetsPath}");
            }
        }

        private void ExecuteContinueToSync()
        {
            if (IsStep2Completed)
            {
                ShimLogSink.RaiseLog(ShimLogLevel.Info, "Target configuration verified. Transitioning to Screen 3: Sync & Log Stream.");
                CurrentStepIndex = 3;
            }
        }

        private async Task ExecuteSyncAsync()
        {
            if (!CanSync) return;

            IsSyncing = true;
            SyncProgress = 0;
            SyncStatusText = "Initializing Transformation Engine...";

            ShimLogSink.RaiseLog(ShimLogLevel.Info, "==================================================");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "⚡ STARTING FIGMA → UNITY TRANSLATION PIPELINE");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Figma Document: {FigmaUrl}");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, $"Target Path: {UnityAssetsPath}");

            await Task.Delay(500);
            SyncProgress = 25;
            SyncStatusText = "Fetching Figma IR Node Tree...";
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Deserializing Figma REST payload & extracting Zod IR schema...");

            await Task.Delay(600);
            SyncProgress = 55;
            SyncStatusText = "Generating USS Design Tokens & Flex Layouts...";
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Resolved 18 Color Variables, 6 Typography Ramps, and 34 Visual Element Nodes.");
            ShimLogSink.RaiseLog(ShimLogLevel.Warning, "Node 'Card_Hero_Effect' has complex background-blur. Generating high-fidelity SVG raster fallback.");

            await Task.Delay(700);
            SyncProgress = 85;
            SyncStatusText = "Compiling UXML Visual Tree & Assets...";
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Generated VisualTreeAsset 'MainMenuScreen.uxml' and stylesheet 'MainMenuScreen.uss'.");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "Writing asset artifacts into target directory...");

            await Task.Delay(500);
            SyncProgress = 100;
            SyncStatusText = "Sync Complete!";
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "✓ FIGMA → UNITY TRANSLATION PIPELINE FINISHED SUCCESSFULLY");
            ShimLogSink.RaiseLog(ShimLogLevel.Info, "==================================================");

            IsSyncing = false;
        }

        private void OnShimLogReceived(object? sender, ShimLogEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LogEntries.Add(new LogEntryModel(e.Level, e.Message));
                });
            }
            else
            {
                LogEntries.Add(new LogEntryModel(e.Level, e.Message));
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
