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
        
        // Create main window first
        MainWindow mainWindow = new();
        this.MainWindow = mainWindow;
        mainWindow.Show();
        
        // Show splash screen 0.2 seconds after main window is loaded
        mainWindow.Loaded += (s, args) =>
        {
            // Use DispatcherTimer to delay splash screen appearance
            DispatcherTimer delayTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(200) // 0.2 seconds
            };
            delayTimer.Tick += (sender, e) =>
            {
                delayTimer.Stop();
                _splashScreen = new SplashScreen();
                _splashScreen.Owner = mainWindow; // Set owner so it appears on top
                _splashScreen.Show();
            };
            delayTimer.Start();
        };
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

