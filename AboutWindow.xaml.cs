using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace PictureWorks;

/// <summary>
/// AboutWindow - Displays information about PictureWorks
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        
        // Share theme resources from MainWindow
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            this.Resources.MergedDictionaries.Add(mainWindow.Resources);
        }
        else
        {
            // Fallback: Use default theme colors
            Resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(240, 244, 248));
            Resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(232, 237, 242));
            Resources["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(74, 144, 226));
            Resources["ButtonHover"] = new SolidColorBrush(Color.FromRgb(53, 122, 189));
            Resources["TextColor"] = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            Resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(189, 195, 199));
        }
    }
    
    private void BtnPayPal_Click(object sender, RoutedEventArgs e)
    {
        // Open PayPal donation URL
        string paypalUrl = "https://paypal.me/soruud";
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = paypalUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening PayPal: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

