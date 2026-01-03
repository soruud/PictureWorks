using System.Diagnostics;
using System.Windows;

namespace PictureWorks;

/// <summary>
/// AboutWindow - Displays information about PictureWorks
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
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

