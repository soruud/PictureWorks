using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

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
        
        // Load image to get its dimensions and set window size
        try
        {
            BitmapImage logo = new();
            logo.BeginInit();
            logo.UriSource = new Uri("pack://application:,,,/PW_LOGO.png");
            logo.EndInit();
            
            // Set window size to match image size
            this.Width = logo.PixelWidth;
            this.Height = logo.PixelHeight;
        }
        catch
        {
            // Fallback size if image can't be loaded
            this.Width = 500;
            this.Height = 300;
        }
        
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

