using System.Windows;
using System.Windows.Threading;

namespace PictureWorks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private SplashScreen? _splashScreen;
    private MainWindow? _mainWindow;
    
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Handle unhandled exceptions
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        // Create main window first (but don't show it yet) to get its position
        _mainWindow = new MainWindow();
        this.MainWindow = _mainWindow;
        
        // Show splash screen centered over where main window will be
        _splashScreen = new SplashScreen();
        
        // Calculate center position based on main window
        double mainLeft = (_mainWindow.Width > 0) ? (SystemParameters.PrimaryScreenWidth - _mainWindow.Width) / 2 : 0;
        double mainTop = (_mainWindow.Height > 0) ? (SystemParameters.PrimaryScreenHeight - _mainWindow.Height) / 2 : 0;
        double mainWidth = _mainWindow.Width > 0 ? _mainWindow.Width : SystemParameters.PrimaryScreenWidth;
        double mainHeight = _mainWindow.Height > 0 ? _mainWindow.Height : SystemParameters.PrimaryScreenHeight;
        
        // Center splash screen over main window position
        _splashScreen.Left = mainLeft + (mainWidth - _splashScreen.Width) / 2;
        _splashScreen.Top = mainTop + (mainHeight - _splashScreen.Height) / 2;
        
        _splashScreen.Show();
        
        // Timer to close splash screen after 3.5 seconds
        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(3.5)
        };
        timer.Tick += (s, args) =>
        {
            timer.Stop();
            
            // Show main window
            _mainWindow.Show();
            
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
