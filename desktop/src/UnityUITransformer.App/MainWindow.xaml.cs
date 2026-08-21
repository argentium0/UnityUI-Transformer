using System.Windows;
using UnityUITransformer.App.ViewModels;

namespace UnityUITransformer.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml with Dependency Injection DataContext wiring
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            TryLoadWindowIcon();
        }

        public MainWindow() : this(new MainViewModel())
        {
        }

        private void TryLoadWindowIcon()
        {
            try
            {
                var iconUri = new System.Uri("pack://application:,,,/Assets/app_icon.ico", System.UriKind.Absolute);
                Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
            }
            catch
            {
                // Ignore icon loading errors gracefully
            }
        }
    }
}