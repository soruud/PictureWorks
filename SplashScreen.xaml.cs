using System.Windows;
using System.Windows.Threading;

namespace PictureWorks;

/// <summary>
/// SplashScreen - Displays logo for 3-5 seconds on application startup
/// </summary>
public partial class SplashScreen : Window
{
    private readonly DispatcherTimer _timer;
    
    public SplashScreen()
    {
        InitializeComponent();
        
        // Create timer to close splash screen after 4.5 seconds
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(4.5)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }
    
    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer.Stop();
        this.Close();
    }
}

