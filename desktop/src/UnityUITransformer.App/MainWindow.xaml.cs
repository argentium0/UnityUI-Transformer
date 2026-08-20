using System.Collections.Specialized;
using System.Windows;
using UnityUITransformer.App.ViewModels;

namespace UnityUITransformer.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainViewModel vm)
            {
                vm.LogEntries.CollectionChanged += OnLogEntriesCollectionChanged;
            }
        }

        private void OnLogEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && DataContext is MainViewModel vm && vm.AutoScrollEnabled)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    TerminalScrollViewer?.ScrollToBottom();
                });
            }
        }
    }
}