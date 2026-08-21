using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using UnityUITransformer.App.Services;
using UnityUITransformer.App.ViewModels;

namespace UnityUITransformer.App
{
    /// <summary>
    /// Interaction logic for App.xaml with Dependency Injection setup and global unhandled exception guards
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SecureStorageService>();
            services.AddSingleton<SupabaseAuthService>();
            services.AddSingleton<FigmaApiService>();
            services.AddSingleton<UxmlGenerator>();
            services.AddSingleton<UssGenerator>();
            services.AddSingleton<ExportService>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogAndShowException(e.Exception);
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        private void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            if (e.Exception != null)
            {
                LogAndShowException(e.Exception);
            }
        }

        private void LogAndShowException(Exception ex)
        {
            ShimLogSink.RaiseLog(ShimLogLevel.Error, $"[UNHANDLED EXCEPTION] {ex.GetType().Name}: {ex.Message}");

            MessageBox.Show(
                "An unexpected error occurred. Please check your connection and try again.\n\nDetails: " + ex.Message,
                "UnityUI Transformer - System Guard",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
