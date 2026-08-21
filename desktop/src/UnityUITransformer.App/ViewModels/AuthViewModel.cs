using System.ComponentModel;
using System.Windows.Input;

namespace UnityUITransformer.App.ViewModels
{
    public class AuthViewModel : ViewModelBase
    {
        public MainViewModel Main { get; }

        public bool IsConnected => Main.IsConnected;
        public bool IsConnecting => Main.IsConnecting;
        public string ConnectedUser => Main.ConnectedUser;
        public string ConnectionStatusText => Main.ConnectionStatusText;
        public ICommand ConnectCommand => Main.ConnectCommand;

        public AuthViewModel(MainViewModel main)
        {
            Main = main;
            Main.PropertyChanged += OnMainPropertyChanged;
        }

        private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsConnected) ||
                e.PropertyName == nameof(MainViewModel.IsConnecting) ||
                e.PropertyName == nameof(MainViewModel.ConnectedUser) ||
                e.PropertyName == nameof(MainViewModel.ConnectionStatusText))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }
    }
}
