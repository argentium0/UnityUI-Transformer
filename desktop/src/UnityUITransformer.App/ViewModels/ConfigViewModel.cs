using System.ComponentModel;
using System.Windows.Input;

namespace UnityUITransformer.App.ViewModels
{
    public class ConfigViewModel : ViewModelBase
    {
        public MainViewModel Main { get; }

        public string FigmaUrl
        {
            get => Main.FigmaUrl;
            set => Main.FigmaUrl = value;
        }

        public string UnityAssetsPath
        {
            get => Main.UnityAssetsPath;
            set => Main.UnityAssetsPath = value;
        }

        public bool IsFigmaUrlValid => Main.IsFigmaUrlValid;
        public bool IsUnityPathValid => Main.IsUnityPathValid;
        public bool IsStep2Completed => Main.IsStep2Completed;

        public string ConfigValidationError => Main.ConfigValidationError;
        public bool HasConfigValidationError => Main.HasConfigValidationError;
        public string ErrorMessage => Main.ErrorMessage;
        public bool HasError => Main.HasError;

        public ICommand BrowseFolderCommand => Main.BrowseFolderCommand;
        public ICommand ContinueToSyncCommand => Main.ContinueToSyncCommand;

        public ConfigViewModel(MainViewModel main)
        {
            Main = main;
            Main.PropertyChanged += OnMainPropertyChanged;
        }

        private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.FigmaUrl) ||
                e.PropertyName == nameof(MainViewModel.UnityAssetsPath) ||
                e.PropertyName == nameof(MainViewModel.IsFigmaUrlValid) ||
                e.PropertyName == nameof(MainViewModel.IsUnityPathValid) ||
                e.PropertyName == nameof(MainViewModel.IsStep2Completed) ||
                e.PropertyName == nameof(MainViewModel.ConfigValidationError) ||
                e.PropertyName == nameof(MainViewModel.HasConfigValidationError) ||
                e.PropertyName == nameof(MainViewModel.ErrorMessage) ||
                e.PropertyName == nameof(MainViewModel.HasError))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }
    }
}
