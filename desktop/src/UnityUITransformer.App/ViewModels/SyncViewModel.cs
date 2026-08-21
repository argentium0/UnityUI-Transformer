using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace UnityUITransformer.App.ViewModels
{
    public class SyncViewModel : ViewModelBase
    {
        public MainViewModel Main { get; }

        public bool CanSync => Main.CanSync;
        public bool IsSyncing => Main.IsSyncing;
        public double SyncProgress => Main.SyncProgress;
        public string SyncStatusText => Main.SyncStatusText;
        public bool AutoScrollEnabled
        {
            get => Main.AutoScrollEnabled;
            set => Main.AutoScrollEnabled = value;
        }

        public ObservableCollection<LogEntryModel> LogEntries => Main.LogEntries;

        public ICommand SyncCommand => Main.SyncCommand;
        public ICommand ClearLogsCommand => Main.ClearLogsCommand;

        public SyncViewModel(MainViewModel main)
        {
            Main = main;
            Main.PropertyChanged += OnMainPropertyChanged;
        }

        private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CanSync) ||
                e.PropertyName == nameof(MainViewModel.IsSyncing) ||
                e.PropertyName == nameof(MainViewModel.SyncProgress) ||
                e.PropertyName == nameof(MainViewModel.SyncStatusText) ||
                e.PropertyName == nameof(MainViewModel.AutoScrollEnabled))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }
    }
}
