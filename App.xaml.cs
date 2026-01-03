using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace PictureWorks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private SplashScreen? _splashScreen;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Handle unhandled exceptions
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        // Show splash screen
        _splashScreen = new SplashScreen();
        _splashScreen.Show();
        
        // Create main window but don't show it yet
        MainWindow mainWindow = new();
        
        // Close splash screen when main window is loaded
        mainWindow.Loaded += (s, args) =>
        {
            // Wait a bit more to ensure splash is visible
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_splashScreen != null)
                {
                    _splashScreen.Close();
                    _splashScreen = null;
                }
            }), DispatcherPriority.Loaded);
        };
        
        this.MainWindow = mainWindow;
        mainWindow.Show();
    }
    
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An error occurred:\n\n{e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
    
    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"A fatal error occurred:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

