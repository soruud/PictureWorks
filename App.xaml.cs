using System.Windows;
using System.Windows.Threading;

namespace PictureWorks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private SplashScreen? _splashScreen;
    
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Handle unhandled exceptions
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        // Show splash screen
        _splashScreen = new SplashScreen();
        _splashScreen.Show();
        
        // Timer to close splash screen after 5 seconds
        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        timer.Tick += (s, args) =>
        {
            timer.Stop();
            
            // Create and show main window
            MainWindow mainWindow = new();
            this.MainWindow = mainWindow;
            mainWindow.Show();
            
            // Close splash screen
            _splashScreen.Close();
            _splashScreen = null;
        };
        timer.Start();
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
