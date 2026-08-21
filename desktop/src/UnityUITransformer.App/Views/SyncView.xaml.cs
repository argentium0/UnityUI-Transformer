using System.Collections.Specialized;
using System.Windows.Controls;
using UnityUITransformer.App.ViewModels;

namespace UnityUITransformer.App.Views
{
    public partial class SyncView : UserControl
    {
        public SyncView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SyncViewModel oldVm)
            {
                ((INotifyCollectionChanged)oldVm.LogEntries).CollectionChanged -= OnLogEntriesChanged;
            }

            if (e.NewValue is SyncViewModel newVm)
            {
                ((INotifyCollectionChanged)newVm.LogEntries).CollectionChanged += OnLogEntriesChanged;
            }
        }

        private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (DataContext is SyncViewModel vm && vm.AutoScrollEnabled && e.Action == NotifyCollectionChangedAction.Add)
            {
                TerminalScrollViewer?.ScrollToEnd();
            }
        }
    }
}
