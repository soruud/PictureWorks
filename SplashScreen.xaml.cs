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
        
        // Center splash screen over owner window
        this.Loaded += (s, e) =>
        {
            if (this.Owner != null)
            {
                this.Left = this.Owner.Left + (this.Owner.Width - this.Width) / 2;
                this.Top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;
            }
        };
        
        // Create timer to close splash screen after 5 seconds
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
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

