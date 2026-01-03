using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace PictureWorks;

/// <summary>
/// MainWindow - PictureWorks Image Editor
/// Provides functionality to resize, crop, rotate, and convert images
/// </summary>
public partial class MainWindow : Window
{
    // ============================================
    // Image Management Variables
    // ============================================
    private BitmapImage? _originalImage;
    private BitmapImage? _currentImage;
    private string? _currentImagePath;
    private bool _isDarkMode = false;
    
    // ============================================
    // Undo/Redo System
    // ============================================
    private readonly Stack<BitmapImage> _undoStack = new();
    private readonly Stack<BitmapImage> _redoStack = new();
    private const int MAX_UNDO_HISTORY = 20;
    
    // ============================================
    // Crop Selection Variables
    // ============================================
    private bool _isCropMode = false;
    private Point _cropStartPoint;
    
    // ============================================
    // Constructor
    // ============================================
    public MainWindow()
    {
        InitializeComponent();
        SetupDragAndDrop();
        
        // Initialize resize UI visibility
        if (RbResizePercent.IsChecked == true)
        {
            PanelPercentSize.Visibility = Visibility.Visible;
            PanelPixelSize.Visibility = Visibility.Collapsed;
        }
        else
        {
            PanelPercentSize.Visibility = Visibility.Collapsed;
            PanelPixelSize.Visibility = Visibility.Visible;
        }
        
        // Update theme after window is loaded
        this.Loaded += (s, e) => UpdateTheme();
    }
    
    // ============================================
    // Drag and Drop Setup
    // ============================================
    private void SetupDragAndDrop()
    {
        this.DragOver += MainWindow_DragOver;
        this.Drop += MainWindow_Drop;
    }
    
    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }
    
    private void MainWindow_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                LoadImage(files[0]);
            }
        }
    }
    
    // ============================================
    // Image Loading Functions
    // ============================================
    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif;*.webp|All Files|*.*",
            Title = "Open Image"
        };
        
        if (dialog.ShowDialog() == true)
        {
            LoadImage(dialog.FileName);
        }
    }
    
    private void LoadImage(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("File not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            _currentImagePath = filePath;
            
            // Load original image
            _originalImage = new BitmapImage();
            _originalImage.BeginInit();
            _originalImage.UriSource = new Uri(filePath);
            _originalImage.CacheOption = BitmapCacheOption.OnLoad;
            _originalImage.EndInit();
            _originalImage.Freeze();
            
            // Set current image to original (clone it)
            _currentImage = CloneBitmapImage(_originalImage);
            _currentImage.Freeze();
            
            // Clear undo/redo stacks
            _undoStack.Clear();
            _redoStack.Clear();
            
            // Update UI
            UpdateImageDisplay();
            EnableImageOperations(true);
            UpdateStatus($"Loaded: {Path.GetFileName(filePath)} ({_originalImage.PixelWidth}x{_originalImage.PixelHeight})");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void UpdateImageDisplay()
    {
        if (_originalImage != null)
        {
            ImgOriginal.Source = _originalImage;
        }
        
        if (_currentImage != null)
        {
            ImgEdited.Source = _currentImage;
            
            // Update canvas size to match image pixel dimensions
            CanvasEdited.Width = _currentImage.PixelWidth;
            CanvasEdited.Height = _currentImage.PixelHeight;
        }
    }
    
    private void EnableImageOperations(bool enable)
    {
        BtnSaveAs.IsEnabled = enable;
        BtnResize.IsEnabled = enable;
        BtnRotateLeft.IsEnabled = enable;
        BtnRotateRight.IsEnabled = enable;
        BtnCrop.IsEnabled = enable;
        BtnCropMoveUp.IsEnabled = enable;
        BtnCropMoveDown.IsEnabled = enable;
        BtnCropMoveLeft.IsEnabled = enable;
        BtnCropMoveRight.IsEnabled = enable;
        BtnCropCenter.IsEnabled = enable;
    }
    
    // ============================================
    // Resize Functions
    // ============================================
    private void RbResizeMode_Changed(object sender, RoutedEventArgs e)
    {
        if (RbResizePercent.IsChecked == true)
        {
            PanelPercentSize.Visibility = Visibility.Visible;
            PanelPixelSize.Visibility = Visibility.Collapsed;
        }
        else if (RbResizePixel.IsChecked == true)
        {
            PanelPercentSize.Visibility = Visibility.Collapsed;
            PanelPixelSize.Visibility = Visibility.Visible;
        }
    }
    
    private void SliderResizePercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtPercentValue != null)
        {
            TxtPercentValue.Text = $"{(int)e.NewValue}%";
        }
    }
    
    private void BtnResize_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        try
        {
            int newWidth, newHeight;
            
            if (RbResizePercent.IsChecked == true)
            {
                // Resize by percentage from slider
                double percent = SliderResizePercent.Value;
                newWidth = (int)(_currentImage.PixelWidth * percent / 100.0);
                newHeight = (int)(_currentImage.PixelHeight * percent / 100.0);
            }
            else
            {
                // Resize by pixel - get width and height from text boxes
                if (!int.TryParse(TxtResizeWidth.Text, out newWidth) || newWidth <= 0)
                {
                    MessageBox.Show("Please enter a valid positive width.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (CbMaintainAspect.IsChecked == true)
                {
                    // Calculate height based on aspect ratio from width
                    double aspectRatio = (double)_currentImage.PixelHeight / _currentImage.PixelWidth;
                    newHeight = (int)(newWidth * aspectRatio);
                    // Update height text box
                    TxtResizeHeight.Text = newHeight.ToString();
                }
                else
                {
                    // Get height from text box
                    if (!int.TryParse(TxtResizeHeight.Text, out newHeight) || newHeight <= 0)
                    {
                        MessageBox.Show("Please enter a valid positive height.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }
            
            ResizeImage(newWidth, newHeight);
            UpdateStatus($"Resized to {newWidth}x{newHeight}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error resizing image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void TxtResizeWidth_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (CbMaintainAspect.IsChecked == true && _currentImage != null)
        {
            if (int.TryParse(TxtResizeWidth.Text, out int width) && width > 0)
            {
                double aspectRatio = (double)_currentImage.PixelHeight / _currentImage.PixelWidth;
                int height = (int)(width * aspectRatio);
                TxtResizeHeight.Text = height.ToString();
            }
        }
    }
    
    private void ResizeImage(int newWidth, int newHeight)
    {
        if (_currentImage == null) return;
        
        // Save to undo stack
        SaveToUndoStack();
        
        // Create resized image using TransformedBitmap
        TransformedBitmap resized = new(_currentImage, new ScaleTransform(
            (double)newWidth / _currentImage.PixelWidth,
            (double)newHeight / _currentImage.PixelHeight));
        
        // Convert to BitmapImage via encoder
        BitmapImage result = ConvertBitmapSourceToBitmapImage(resized);
        result.Freeze();
        
        _currentImage = result;
        UpdateImageDisplay();
        ClearRedoStack();
    }
    
    private BitmapImage ConvertBitmapSourceToBitmapImage(BitmapSource bitmapSource)
    {
        BitmapImage bitmapImage = new();
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        
        using (MemoryStream stream = new())
        {
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
        }
        
        return bitmapImage;
    }
    
    // ============================================
    // Rotate Functions
    // ============================================
    private void BtnRotateLeft_Click(object sender, RoutedEventArgs e)
    {
        RotateImage(-90);
    }
    
    private void BtnRotateRight_Click(object sender, RoutedEventArgs e)
    {
        RotateImage(90);
    }
    
    private void RotateImage(double angle)
    {
        if (_currentImage == null) return;
        
        SaveToUndoStack();
        
        // Create rotation transform centered on image
        RotateTransform rotateTransform = new(angle, _currentImage.PixelWidth / 2.0, _currentImage.PixelHeight / 2.0);
        TransformedBitmap rotated = new(_currentImage, rotateTransform);
        
        // Convert to BitmapImage
        BitmapImage result = ConvertBitmapSourceToBitmapImage(rotated);
        result.Freeze();
        
        _currentImage = result;
        UpdateImageDisplay();
        ClearRedoStack();
        UpdateStatus($"Rotated {angle} degrees");
    }
    
    // ============================================
    // Crop Functions
    // ============================================
    private void CanvasEdited_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentImage == null) return;
        
        _isCropMode = true;
        _cropStartPoint = e.GetPosition(CanvasEdited);
        RectCropSelection.Visibility = Visibility.Visible;
        Canvas.SetLeft(RectCropSelection, _cropStartPoint.X);
        Canvas.SetTop(RectCropSelection, _cropStartPoint.Y);
        RectCropSelection.Width = 0;
        RectCropSelection.Height = 0;
        
        CanvasEdited.CaptureMouse();
    }
    
    private void CanvasEdited_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentImage == null || !_isCropMode || !CanvasEdited.IsMouseCaptured) return;
        
        Point currentPoint = e.GetPosition(CanvasEdited);
        double width = currentPoint.X - _cropStartPoint.X;
        double height = currentPoint.Y - _cropStartPoint.Y;
        
        RectCropSelection.Width = Math.Abs(width);
        RectCropSelection.Height = Math.Abs(height);
        
        if (width < 0)
            Canvas.SetLeft(RectCropSelection, currentPoint.X);
        else
            Canvas.SetLeft(RectCropSelection, _cropStartPoint.X);
            
        if (height < 0)
            Canvas.SetTop(RectCropSelection, currentPoint.Y);
        else
            Canvas.SetTop(RectCropSelection, _cropStartPoint.Y);
    }
    
    private void CanvasEdited_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentImage == null || !_isCropMode) return;
        
        CanvasEdited.ReleaseMouseCapture();
        // Crop selection is now visible and ready for Apply Crop button
    }
    
    private void BtnCrop_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        if (RectCropSelection.Visibility != Visibility.Visible || 
            RectCropSelection.Width <= 0 || RectCropSelection.Height <= 0)
        {
            MessageBox.Show("Please select an area to crop by dragging on the edited image.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        ApplyCrop();
    }
    
    private void ApplyCrop()
    {
        if (_currentImage == null) return;
        
        SaveToUndoStack();
        
        // Calculate scale factors (Canvas has pixel dimensions, Viewbox scales it)
        // CanvasEdited.Width and Height are set to image pixel dimensions
        double scaleX = _currentImage.PixelWidth / CanvasEdited.Width;
        double scaleY = _currentImage.PixelHeight / CanvasEdited.Height;
        
        // Get crop rectangle from canvas
        double canvasX = Canvas.GetLeft(RectCropSelection);
        double canvasY = Canvas.GetTop(RectCropSelection);
        double canvasWidth = RectCropSelection.Width;
        double canvasHeight = RectCropSelection.Height;
        
        // Convert to image coordinates
        int x = (int)(canvasX * scaleX);
        int y = (int)(canvasY * scaleY);
        int width = (int)(canvasWidth * scaleX);
        int height = (int)(canvasHeight * scaleY);
        
        // Ensure crop is within image bounds
        x = Math.Max(0, Math.Min(x, _currentImage.PixelWidth - 1));
        y = Math.Max(0, Math.Min(y, _currentImage.PixelHeight - 1));
        width = Math.Min(width, _currentImage.PixelWidth - x);
        height = Math.Min(height, _currentImage.PixelHeight - y);
        
        if (width <= 0 || height <= 0)
        {
            MessageBox.Show("Invalid crop selection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        // Create cropped image
        CroppedBitmap cropped = new(_currentImage, new Int32Rect(x, y, width, height));
        
        // Convert to BitmapImage
        BitmapImage result = ConvertBitmapSourceToBitmapImage(cropped);
        result.Freeze();
        
        _currentImage = result;
        _isCropMode = false;
        RectCropSelection.Visibility = Visibility.Collapsed;
        UpdateImageDisplay();
        ClearRedoStack();
        UpdateStatus($"Cropped to {width}x{height}");
    }
    
    // ============================================
    // Crop Movement Functions
    // ============================================
    private void MoveCropSelection(double deltaX, double deltaY)
    {
        if (RectCropSelection.Visibility != Visibility.Visible || _currentImage == null) return;
        
        double currentX = Canvas.GetLeft(RectCropSelection);
        double currentY = Canvas.GetTop(RectCropSelection);
        double newX = currentX + deltaX;
        double newY = currentY + deltaY;
        
        // Ensure crop stays within canvas bounds
        newX = Math.Max(0, Math.Min(newX, CanvasEdited.Width - RectCropSelection.Width));
        newY = Math.Max(0, Math.Min(newY, CanvasEdited.Height - RectCropSelection.Height));
        
        Canvas.SetLeft(RectCropSelection, newX);
        Canvas.SetTop(RectCropSelection, newY);
    }
    
    private void BtnCropMoveUp_Click(object sender, RoutedEventArgs e)
    {
        MoveCropSelection(0, -10);
    }
    
    private void BtnCropMoveDown_Click(object sender, RoutedEventArgs e)
    {
        MoveCropSelection(0, 10);
    }
    
    private void BtnCropMoveLeft_Click(object sender, RoutedEventArgs e)
    {
        MoveCropSelection(-10, 0);
    }
    
    private void BtnCropMoveRight_Click(object sender, RoutedEventArgs e)
    {
        MoveCropSelection(10, 0);
    }
    
    private void BtnCropCenter_Click(object sender, RoutedEventArgs e)
    {
        if (RectCropSelection.Visibility != Visibility.Visible || _currentImage == null) return;
        
        // Center the crop selection on the canvas
        double centerX = (CanvasEdited.Width - RectCropSelection.Width) / 2;
        double centerY = (CanvasEdited.Height - RectCropSelection.Height) / 2;
        
        Canvas.SetLeft(RectCropSelection, Math.Max(0, centerX));
        Canvas.SetTop(RectCropSelection, Math.Max(0, centerY));
    }
    
    // ============================================
    // Save Functions
    // ============================================
    private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        SaveFileDialog dialog = new()
        {
            Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|BMP Image|*.bmp|GIF Image|*.gif|TIFF Image|*.tiff;*.tif|WebP Image|*.webp|All Files|*.*",
            Title = "Save Image As"
        };
        
        if (dialog.ShowDialog() == true)
        {
            SaveImage(_currentImage, dialog.FileName);
        }
    }
    
    private void SaveImage(BitmapImage image, string filePath)
    {
        try
        {
            string extension = Path.GetExtension(filePath).ToLower();
            BitmapEncoder encoder = extension switch
            {
                ".png" => new PngBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                ".bmp" => new BmpBitmapEncoder(),
                ".gif" => new GifBitmapEncoder(),
                ".tiff" or ".tif" => new TiffBitmapEncoder(),
                ".webp" => new PngBitmapEncoder(), // WebP not directly supported, use PNG as fallback
                _ => new PngBitmapEncoder()
            };
            
            encoder.Frames.Add(BitmapFrame.Create(image));
            
            using (FileStream stream = new(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
            
            UpdateStatus($"Saved: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // ============================================
    // Undo/Redo Functions
    // ============================================
    private void SaveToUndoStack()
    {
        if (_currentImage == null) return;
        
        _undoStack.Push(CloneBitmapImage(_currentImage));
        if (_undoStack.Count > MAX_UNDO_HISTORY)
        {
            // Remove oldest
            Stack<BitmapImage> temp = new(_undoStack);
            _undoStack.Clear();
            for (int i = 0; i < MAX_UNDO_HISTORY; i++)
            {
                _undoStack.Push(temp.Pop());
            }
        }
        
        BtnUndo.IsEnabled = _undoStack.Count > 0;
    }
    
    private void ClearRedoStack()
    {
        _redoStack.Clear();
        BtnRedo.IsEnabled = false;
    }
    
    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        if (_undoStack.Count == 0 || _currentImage == null) return;
        
        _redoStack.Push(CloneBitmapImage(_currentImage));
        _currentImage = _undoStack.Pop();
        UpdateImageDisplay();
        
        BtnUndo.IsEnabled = _undoStack.Count > 0;
        BtnRedo.IsEnabled = _redoStack.Count > 0;
        UpdateStatus("Undo");
    }
    
    private void BtnRedo_Click(object sender, RoutedEventArgs e)
    {
        if (_redoStack.Count == 0) return;
        
        if (_currentImage != null)
        {
            _undoStack.Push(CloneBitmapImage(_currentImage));
        }
        
        _currentImage = _redoStack.Pop();
        UpdateImageDisplay();
        
        BtnUndo.IsEnabled = _undoStack.Count > 0;
        BtnRedo.IsEnabled = _redoStack.Count > 0;
        UpdateStatus("Redo");
    }
    
    // ============================================
    // Theme Functions
    // ============================================
    private void BtnDarkMode_Click(object sender, RoutedEventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        UpdateTheme();
    }
    
    private void UpdateTheme()
    {
        try
        {
            if (_isDarkMode)
            {
                if (Resources.Contains("WindowBackgroundDark"))
                {
                    Resources["WindowBackground"] = Resources["WindowBackgroundDark"];
                    Resources["PanelBackground"] = Resources["PanelBackgroundDark"];
                    Resources["ButtonBackground"] = Resources["ButtonBackgroundDark"];
                    Resources["ButtonHover"] = Resources["ButtonHoverDark"];
                    Resources["TextColor"] = Resources["TextColorDark"];
                    Resources["BorderColor"] = Resources["BorderColorDark"];
                }
                if (BtnDarkMode != null)
                    BtnDarkMode.Content = "Light Mode";
            }
            else
            {
                Resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(240, 244, 248));
                Resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(232, 237, 242));
                Resources["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(74, 144, 226));
                Resources["ButtonHover"] = new SolidColorBrush(Color.FromRgb(53, 122, 189));
                Resources["TextColor"] = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                Resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(189, 195, 199));
                if (BtnDarkMode != null)
                    BtnDarkMode.Content = "Dark Mode";
            }
        }
        catch (Exception ex)
        {
            // Silently fail - theme will use defaults
            System.Diagnostics.Debug.WriteLine($"Theme update error: {ex.Message}");
        }
    }
    
    // ============================================
    // Utility Functions
    // ============================================
    private void UpdateStatus(string message)
    {
        TxtStatus.Text = message;
    }
    
    /// <summary>
    /// Clone a BitmapImage by encoding and decoding it
    /// </summary>
    private BitmapImage CloneBitmapImage(BitmapImage source)
    {
        if (source == null) return null!;
        
        BitmapImage cloned = new();
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(source));
        
        using (MemoryStream stream = new())
        {
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            cloned.BeginInit();
            cloned.StreamSource = stream;
            cloned.CacheOption = BitmapCacheOption.OnLoad;
            cloned.EndInit();
            cloned.Freeze();
        }
        
        return cloned;
    }
}
