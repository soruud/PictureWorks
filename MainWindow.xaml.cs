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
/// MainWindow - PictureWorks Image Editor v2.0.4
/// Provides functionality to resize, crop, rotate, flip, adjust brightness/contrast,
/// apply filters, remove backgrounds, and convert images
/// </summary>
public partial class MainWindow : Window
{
    // ============================================
    // Image Management Variables
    // ============================================
    private BitmapImage? _originalImage;
    private BitmapImage? _currentImage;
    private string? _currentImagePath;
    private bool _isDarkMode = true;
    
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
    private bool _isMovingCrop = false;
    private Point _cropStartPoint;
    private Point _cropMoveStartPoint;
    
    // ============================================
    // Zoom Variables
    // ============================================
    private double _zoomLevel = 1.0;
    private const double MIN_ZOOM = 0.1;
    private const double MAX_ZOOM = 5.0;
    private const double ZOOM_STEP = 0.1;
    
    // ============================================
    // Background Removal Variables
    // ============================================
    private bool _isPickingColor = false;
    private Color _selectedColorToRemove = Colors.White;
    
    // ============================================
    // Constructor
    // ============================================
    public MainWindow()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing window:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
        
        try
        {
            SetupDragAndDrop();
            SetupKeyboardShortcuts();
            
            // Attach event handlers after InitializeComponent
            RbResizePercent.Checked += RbResizeMode_Changed;
            RbResizePercent.Unchecked += RbResizeMode_Changed;
            RbResizePixel.Checked += RbResizeMode_Changed;
            RbResizePixel.Unchecked += RbResizeMode_Changed;
            TxtResizeWidth.TextChanged += TxtResizeWidth_TextChanged;
            
            // Slider value changed handlers
            SliderTolerance.ValueChanged += (s, e) => TxtToleranceValue.Text = ((int)SliderTolerance.Value).ToString();
            
            // Attach keyboard event handler for arrow keys
            this.KeyDown += MainWindow_KeyDown;
            this.Focusable = true;
            
            // Initialize UI after all controls are loaded
            this.Loaded += (s, e) => 
            {
                try
                {
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
                    
                    UpdateTheme();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
                }
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error setting up window:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // ============================================
    // Keyboard Shortcuts Setup
    // ============================================
    private void SetupKeyboardShortcuts()
    {
        // Ctrl+V to paste, Ctrl+C to copy
        this.PreviewKeyDown += (s, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.V)
                {
                    PasteFromClipboard();
                    e.Handled = true;
                }
                else if (e.Key == Key.C && _currentImage != null)
                {
                    CopyToClipboard();
                    e.Handled = true;
                }
            }
        };
    }
    
    // ============================================
    // Clipboard Functions
    // ============================================
    private void PasteFromClipboard()
    {
        try
        {
            if (Clipboard.ContainsImage())
            {
                BitmapSource clipboardImage = Clipboard.GetImage();
                if (clipboardImage != null)
                {
                    // Convert to BitmapImage
                    _currentImage = ConvertBitmapSourceToBitmapImage(clipboardImage);
                    _currentImage.Freeze();
                    _originalImage = CloneBitmapImage(_currentImage);
                    
                    // Reset zoom and stacks
                    _zoomLevel = 1.0;
                    _undoStack.Clear();
                    _redoStack.Clear();
                    
                    // Update UI
                    UpdateImageDisplay();
                    EnableImageOperations(true);
                    
                    // Update crop size
                    TxtCropWidth.Text = _currentImage.PixelWidth.ToString();
                    TxtCropHeight.Text = _currentImage.PixelHeight.ToString();
                    
                    UpdateStatus($"Pasted from clipboard ({_currentImage.PixelWidth}x{_currentImage.PixelHeight})");
                }
            }
            else
            {
                UpdateStatus("No image in clipboard");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error pasting from clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void CopyToClipboard()
    {
        try
        {
            if (_currentImage != null)
            {
                Clipboard.SetImage(_currentImage);
                UpdateStatus("Copied to clipboard");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            
            // Reset zoom level when loading new image
            _zoomLevel = 1.0;
            
            // Clear undo/redo stacks
            _undoStack.Clear();
            _redoStack.Clear();
            
            // Update UI
            UpdateImageDisplay();
            EnableImageOperations(true);
            
            // Update crop size text boxes with current image dimensions
            if (TxtCropWidth != null && TxtCropHeight != null)
            {
                TxtCropWidth.Text = _currentImage.PixelWidth.ToString();
                TxtCropHeight.Text = _currentImage.PixelHeight.ToString();
            }
            
            UpdateStatus($"Loaded: {Path.GetFileName(filePath)} ({_originalImage.PixelWidth}x{_originalImage.PixelHeight})");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void UpdateImageDisplay()
    {
        if (_currentImage != null)
        {
            ImgEdited.Source = _currentImage;
            ImgEdited.RenderTransform = null;
            ApplyZoom();
            
            if (!ImgEdited.IsLoaded)
            {
                ImgEdited.Loaded += (s, e) => UpdateCanvasSize();
            }
            else
            {
                UpdateCanvasSize();
            }
            
            this.SizeChanged += (s, e) =>
            {
                if (_currentImage != null)
                {
                    UpdateCanvasSize();
                }
            };
        }
    }
    
    private void ApplyZoom()
    {
        if (ImgEdited != null && _currentImage != null)
        {
            ScaleTransform scaleTransform = new(_zoomLevel, _zoomLevel);
            ImgEdited.RenderTransform = scaleTransform;
            ImgEdited.RenderTransformOrigin = new Point(0, 0);
        }
    }
    
    private void UpdateCanvasSize()
    {
        if (ImgEdited.ActualWidth > 0 && ImgEdited.ActualHeight > 0)
        {
            CanvasEdited.Width = ImgEdited.ActualWidth * _zoomLevel;
            CanvasEdited.Height = ImgEdited.ActualHeight * _zoomLevel;
        }
        else
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ImgEdited.ActualWidth > 0 && ImgEdited.ActualHeight > 0)
                {
                    CanvasEdited.Width = ImgEdited.ActualWidth * _zoomLevel;
                    CanvasEdited.Height = ImgEdited.ActualHeight * _zoomLevel;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
    
    // ============================================
    // Zoom Functions
    // ============================================
    private void CanvasEdited_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_currentImage == null) return;
        
        if (e.Delta > 0)
            _zoomLevel = Math.Min(MAX_ZOOM, _zoomLevel + ZOOM_STEP);
        else
            _zoomLevel = Math.Max(MIN_ZOOM, _zoomLevel - ZOOM_STEP);
        
        ApplyZoom();
        UpdateCanvasSize();
        e.Handled = true;
    }
    
    private void EnableImageOperations(bool enable)
    {
        BtnSaveAs.IsEnabled = enable;
        BtnResize.IsEnabled = enable;
        BtnRotateLeft.IsEnabled = enable;
        BtnRotateRight.IsEnabled = enable;
        BtnFlipH.IsEnabled = enable;
        BtnFlipV.IsEnabled = enable;
        BtnCrop.IsEnabled = enable;
        BtnCropMoveUp.IsEnabled = enable;
        BtnCropMoveDown.IsEnabled = enable;
        BtnCropMoveLeft.IsEnabled = enable;
        BtnCropMoveRight.IsEnabled = enable;
        BtnCropCenter.IsEnabled = enable;
        BtnGrayscale.IsEnabled = enable;
        BtnSepia.IsEnabled = enable;
        BtnResetAdjustments.IsEnabled = enable;
        BtnPickColor.IsEnabled = enable;
        BtnRemoveBG.IsEnabled = enable;
    }
    
    // ============================================
    // Flip Functions
    // ============================================
    private void BtnFlipH_Click(object sender, RoutedEventArgs e)
    {
        FlipImage(true);
    }
    
    private void BtnFlipV_Click(object sender, RoutedEventArgs e)
    {
        FlipImage(false);
    }
    
    private void FlipImage(bool horizontal)
    {
        if (_currentImage == null) return;
        
        SaveToUndoStack();
        
        ScaleTransform flipTransform = horizontal 
            ? new ScaleTransform(-1, 1, _currentImage.PixelWidth / 2.0, 0)
            : new ScaleTransform(1, -1, 0, _currentImage.PixelHeight / 2.0);
        
        TransformedBitmap flipped = new(_currentImage, flipTransform);
        BitmapImage result = ConvertBitmapSourceToBitmapImage(flipped);
        result.Freeze();
        
        _currentImage = result;
        UpdateImageDisplay();
        ClearRedoStack();
        UpdateStatus($"Flipped {(horizontal ? "horizontally" : "vertically")}");
    }
    
    // ============================================
    // Brightness/Contrast Functions (Live Preview)
    // ============================================
    private BitmapImage? _imageBeforeAdjustments;
    private bool _isAdjusting = false;
    
    private void Slider_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is Slider slider)
        {
            double change = e.Delta > 0 ? 5 : -5;
            slider.Value = Math.Max(slider.Minimum, Math.Min(slider.Maximum, slider.Value + change));
            e.Handled = true;
        }
    }
    
    private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtBrightnessValue != null)
            TxtBrightnessValue.Text = ((int)SliderBrightness.Value).ToString();
        
        ApplyLiveAdjustments();
    }
    
    private void SliderContrast_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtContrastValue != null)
            TxtContrastValue.Text = ((int)SliderContrast.Value).ToString();
        
        ApplyLiveAdjustments();
    }
    
    private void ApplyLiveAdjustments()
    {
        if (_currentImage == null || _isAdjusting) return;
        if (SliderBrightness == null || SliderContrast == null) return;
        
        // Store original image before first adjustment
        if (_imageBeforeAdjustments == null)
        {
            _imageBeforeAdjustments = CloneBitmapImage(_currentImage);
            SaveToUndoStack();
        }
        
        double brightness = SliderBrightness.Value / 100.0;
        double contrast = (SliderContrast.Value + 100) / 100.0;
        
        // Apply adjustments from original
        _isAdjusting = true;
        ApplyBrightnessContrastFromSource(_imageBeforeAdjustments, brightness, contrast);
        _isAdjusting = false;
    }
    
    private void BtnResetAdjustments_Click(object sender, RoutedEventArgs e)
    {
        if (_imageBeforeAdjustments != null)
        {
            _currentImage = CloneBitmapImage(_imageBeforeAdjustments);
            _currentImage.Freeze();
            UpdateImageDisplay();
            _imageBeforeAdjustments = null;
        }
        
        _isAdjusting = true;
        SliderBrightness.Value = 0;
        SliderContrast.Value = 0;
        _isAdjusting = false;
        
        ClearRedoStack();
        UpdateStatus("Adjustments reset");
    }
    
    private void ApplyBrightnessContrastFromSource(BitmapImage source, double brightness, double contrast)
    {
        if (source == null) return;
        
        WriteableBitmap writeable = new(source);
        writeable.Lock();
        
        int width = writeable.PixelWidth;
        int height = writeable.PixelHeight;
        int stride = writeable.BackBufferStride;
        
        unsafe
        {
            byte* buffer = (byte*)writeable.BackBuffer;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    
                    for (int c = 0; c < 3; c++)
                    {
                        double value = buffer[index + c] / 255.0;
                        value = (value - 0.5) * contrast + 0.5;
                        value += brightness;
                        value = Math.Max(0, Math.Min(1, value));
                        buffer[index + c] = (byte)(value * 255);
                    }
                }
            }
        }
        
        writeable.AddDirtyRect(new Int32Rect(0, 0, width, height));
        writeable.Unlock();
        
        _currentImage = ConvertBitmapSourceToBitmapImage(writeable);
        _currentImage.Freeze();
        UpdateImageDisplay();
    }
    
    
    // ============================================
    // Filter Functions (Grayscale, Sepia) - Toggle On/Off
    // ============================================
    private BitmapImage? _imageBeforeFilter;
    private bool _isGrayscaleOn = false;
    private bool _isSepiaOn = false;
    
    private void BtnGrayscale_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        if (_isGrayscaleOn)
        {
            // Turn off - restore original
            if (_imageBeforeFilter != null)
            {
                _currentImage = CloneBitmapImage(_imageBeforeFilter);
                _currentImage.Freeze();
                UpdateImageDisplay();
                _imageBeforeFilter = null;
            }
            _isGrayscaleOn = false;
            BtnGrayscale.Content = "Grayscale";
            UpdateStatus("Grayscale filter removed");
        }
        else
        {
            // Turn off sepia first if on
            if (_isSepiaOn)
            {
                _isSepiaOn = false;
                BtnSepia.Content = "Sepia";
            }
            
            // Store original and apply
            if (_imageBeforeFilter == null)
            {
                _imageBeforeFilter = CloneBitmapImage(_currentImage);
                SaveToUndoStack();
            }
            
            ApplyGrayscale();
            _isGrayscaleOn = true;
            BtnGrayscale.Content = "Grayscale ✓";
            ClearRedoStack();
            UpdateStatus("Grayscale filter applied (click again to remove)");
        }
    }
    
    private void BtnSepia_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        if (_isSepiaOn)
        {
            // Turn off - restore original
            if (_imageBeforeFilter != null)
            {
                _currentImage = CloneBitmapImage(_imageBeforeFilter);
                _currentImage.Freeze();
                UpdateImageDisplay();
                _imageBeforeFilter = null;
            }
            _isSepiaOn = false;
            BtnSepia.Content = "Sepia";
            UpdateStatus("Sepia filter removed");
        }
        else
        {
            // Turn off grayscale first if on
            if (_isGrayscaleOn)
            {
                _isGrayscaleOn = false;
                BtnGrayscale.Content = "Grayscale";
            }
            
            // Store original and apply
            if (_imageBeforeFilter == null)
            {
                _imageBeforeFilter = CloneBitmapImage(_currentImage);
                SaveToUndoStack();
            }
            
            ApplySepia();
            _isSepiaOn = true;
            BtnSepia.Content = "Sepia ✓";
            ClearRedoStack();
            UpdateStatus("Sepia filter applied (click again to remove)");
        }
    }
    
    private void ApplyGrayscale()
    {
        if (_imageBeforeFilter == null) return;
        
        FormatConvertedBitmap grayscale = new();
        grayscale.BeginInit();
        grayscale.Source = _imageBeforeFilter;
        grayscale.DestinationFormat = PixelFormats.Gray32Float;
        grayscale.EndInit();
        
        FormatConvertedBitmap bgra = new();
        bgra.BeginInit();
        bgra.Source = grayscale;
        bgra.DestinationFormat = PixelFormats.Bgra32;
        bgra.EndInit();
        
        _currentImage = ConvertBitmapSourceToBitmapImage(bgra);
        _currentImage.Freeze();
        UpdateImageDisplay();
    }
    
    private void ApplySepia()
    {
        if (_imageBeforeFilter == null) return;
        
        WriteableBitmap writeable = new(_imageBeforeFilter);
        writeable.Lock();
        
        int width = writeable.PixelWidth;
        int height = writeable.PixelHeight;
        int stride = writeable.BackBufferStride;
        
        unsafe
        {
            byte* buffer = (byte*)writeable.BackBuffer;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    
                    byte b = buffer[index];
                    byte g = buffer[index + 1];
                    byte r = buffer[index + 2];
                    
                    int newR = (int)(r * 0.393 + g * 0.769 + b * 0.189);
                    int newG = (int)(r * 0.349 + g * 0.686 + b * 0.168);
                    int newB = (int)(r * 0.272 + g * 0.534 + b * 0.131);
                    
                    buffer[index] = (byte)Math.Min(255, newB);
                    buffer[index + 1] = (byte)Math.Min(255, newG);
                    buffer[index + 2] = (byte)Math.Min(255, newR);
                }
            }
        }
        
        writeable.AddDirtyRect(new Int32Rect(0, 0, width, height));
        writeable.Unlock();
        
        _currentImage = ConvertBitmapSourceToBitmapImage(writeable);
        _currentImage.Freeze();
        UpdateImageDisplay();
    }
    
    // ============================================
    // Background Removal Functions
    // ============================================
    private void BtnPickColor_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        _isPickingColor = true;
        UpdateStatus("Click on the image to select a color to remove");
        this.Cursor = Cursors.Cross;
    }
    
    private void BtnRemoveBG_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        SaveToUndoStack();
        RemoveBackgroundColor(_selectedColorToRemove, (int)SliderTolerance.Value);
        ClearRedoStack();
        UpdateStatus($"Removed background color (tolerance: {(int)SliderTolerance.Value})");
    }
    
    private void RemoveBackgroundColor(Color colorToRemove, int tolerance)
    {
        if (_currentImage == null) return;
        
        // First convert to BGRA32 format to ensure we have an alpha channel
        FormatConvertedBitmap formattedSource = new();
        formattedSource.BeginInit();
        formattedSource.Source = _currentImage;
        formattedSource.DestinationFormat = PixelFormats.Bgra32;
        formattedSource.EndInit();
        
        // Create WriteableBitmap from the BGRA32 source
        WriteableBitmap writeable = new(formattedSource);
        writeable.Lock();
        
        int width = writeable.PixelWidth;
        int height = writeable.PixelHeight;
        int stride = writeable.BackBufferStride;
        int pixelsChanged = 0;
        
        unsafe
        {
            byte* buffer = (byte*)writeable.BackBuffer;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    
                    byte b = buffer[index];
                    byte g = buffer[index + 1];
                    byte r = buffer[index + 2];
                    
                    // Calculate color distance
                    int distance = Math.Abs(r - colorToRemove.R) + 
                                   Math.Abs(g - colorToRemove.G) + 
                                   Math.Abs(b - colorToRemove.B);
                    
                    // If within tolerance, make transparent
                    if (distance <= tolerance * 3) // tolerance * 3 because we sum 3 channels
                    {
                        buffer[index + 3] = 0; // Set alpha to 0 (transparent)
                        pixelsChanged++;
                    }
                }
            }
        }
        
        writeable.AddDirtyRect(new Int32Rect(0, 0, width, height));
        writeable.Unlock();
        
        _currentImage = ConvertBitmapSourceToBitmapImage(writeable);
        _currentImage.Freeze();
        UpdateImageDisplay();
        
        UpdateStatus($"Removed {pixelsChanged:N0} pixels (tolerance: {tolerance})");
    }
    
    // ============================================
    // Aspect Ratio Functions
    // ============================================
    private void CbAspectRatio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentImage == null || CbAspectRatio.SelectedItem == null) return;
        
        string selected = ((ComboBoxItem)CbAspectRatio.SelectedItem).Content.ToString() ?? "Free";
        
        if (selected == "Free") return;
        
        // Parse aspect ratio
        double aspectRatio = selected switch
        {
            "16:9" => 16.0 / 9.0,
            "4:3" => 4.0 / 3.0,
            "1:1" => 1.0,
            "3:2" => 3.0 / 2.0,
            "2:3" => 2.0 / 3.0,
            "9:16" => 9.0 / 16.0,
            _ => 1.0
        };
        
        // Calculate new crop dimensions maintaining aspect ratio
        if (int.TryParse(TxtCropWidth.Text, out int width))
        {
            int newHeight = (int)(width / aspectRatio);
            TxtCropHeight.Text = newHeight.ToString();
            SetCropSizeFromInput();
        }
    }
    
    // ============================================
    // Resize Functions
    // ============================================
    private void RbResizeMode_Changed(object sender, RoutedEventArgs e)
    {
        if (PanelPercentSize == null || PanelPixelSize == null) return;
        
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
    
    private void BtnResize_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        try
        {
            int newWidth, newHeight;
            
            if (RbResizePercent.IsChecked == true)
            {
                if (!double.TryParse(TxtResizePercent.Text, out double percent) || percent <= 0)
                {
                    MessageBox.Show("Please enter a valid positive percentage.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                newWidth = (int)(_currentImage.PixelWidth * percent / 100.0);
                newHeight = (int)(_currentImage.PixelHeight * percent / 100.0);
            }
            else
            {
                if (!int.TryParse(TxtResizeWidth.Text, out newWidth) || newWidth <= 0)
                {
                    MessageBox.Show("Please enter a valid positive width.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (CbMaintainAspect.IsChecked == true)
                {
                    double aspectRatio = (double)_currentImage.PixelHeight / _currentImage.PixelWidth;
                    newHeight = (int)(newWidth * aspectRatio);
                    TxtResizeHeight.Text = newHeight.ToString();
                }
                else
                {
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
        if (CbMaintainAspect == null || TxtResizeHeight == null || _currentImage == null) return;
        
        if (CbMaintainAspect.IsChecked == true)
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
        
        SaveToUndoStack();
        
        RenderTargetBitmap renderTarget = new(newWidth, newHeight, 96, 96, PixelFormats.Pbgra32);
        DrawingVisual drawingVisual = new();
        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawImage(_currentImage, new Rect(0, 0, newWidth, newHeight));
        }
        renderTarget.Render(drawingVisual);
        
        BitmapImage result = ConvertBitmapSourceToBitmapImage(renderTarget);
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
        
        RotateTransform rotateTransform = new(angle, _currentImage.PixelWidth / 2.0, _currentImage.PixelHeight / 2.0);
        TransformedBitmap rotated = new(_currentImage, rotateTransform);
        
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
        
        Point mousePos = e.GetPosition(CanvasEdited);
        
        // Handle color picking for background removal
        if (_isPickingColor && e.LeftButton == MouseButtonState.Pressed)
        {
            PickColorFromImage(mousePos);
            return;
        }
        
        // Right mouse button: Create new crop selection
        if (e.RightButton == MouseButtonState.Pressed)
        {
            _isCropMode = true;
            _isMovingCrop = false;
            _cropStartPoint = mousePos;
            RectCropSelection.Visibility = Visibility.Visible;
            Canvas.SetLeft(RectCropSelection, _cropStartPoint.X);
            Canvas.SetTop(RectCropSelection, _cropStartPoint.Y);
            RectCropSelection.Width = 0;
            RectCropSelection.Height = 0;
            
            CanvasEdited.CaptureMouse();
        }
        // Left mouse button: Move existing crop selection
        else if (e.LeftButton == MouseButtonState.Pressed && RectCropSelection.Visibility == Visibility.Visible)
        {
            double cropX = Canvas.GetLeft(RectCropSelection);
            double cropY = Canvas.GetTop(RectCropSelection);
            if (mousePos.X >= cropX && mousePos.X <= cropX + RectCropSelection.Width &&
                mousePos.Y >= cropY && mousePos.Y <= cropY + RectCropSelection.Height)
            {
                _isMovingCrop = true;
                _cropMoveStartPoint = mousePos;
                CanvasEdited.CaptureMouse();
            }
        }
    }
    
    private void PickColorFromImage(Point canvasPos)
    {
        if (_currentImage == null) return;
        
        try
        {
            // Get the position relative to the Image control
            Point imagePos = new(canvasPos.X / _zoomLevel, canvasPos.Y / _zoomLevel);
            
            // Calculate the actual displayed size of the image
            double displayWidth = ImgEdited.ActualWidth;
            double displayHeight = ImgEdited.ActualHeight;
            
            // If ActualWidth/Height are not ready, use pixel dimensions
            if (displayWidth <= 0 || displayHeight <= 0)
            {
                displayWidth = _currentImage.PixelWidth;
                displayHeight = _currentImage.PixelHeight;
            }
            
            // Convert to pixel coordinates
            int imageX = (int)(imagePos.X * _currentImage.PixelWidth / displayWidth);
            int imageY = (int)(imagePos.Y * _currentImage.PixelHeight / displayHeight);
            
            // Ensure within bounds
            imageX = Math.Max(0, Math.Min(imageX, _currentImage.PixelWidth - 1));
            imageY = Math.Max(0, Math.Min(imageY, _currentImage.PixelHeight - 1));
            
            // Convert to BGRA format for reliable pixel reading
            FormatConvertedBitmap formattedBitmap = new();
            formattedBitmap.BeginInit();
            formattedBitmap.Source = _currentImage;
            formattedBitmap.DestinationFormat = PixelFormats.Bgra32;
            formattedBitmap.EndInit();
            
            // Read the pixel
            int stride = formattedBitmap.PixelWidth * 4;
            byte[] pixelData = new byte[4];
            formattedBitmap.CopyPixels(new Int32Rect(imageX, imageY, 1, 1), pixelData, 4, 0);
            
            // BGRA format: Blue=0, Green=1, Red=2, Alpha=3
            _selectedColorToRemove = Color.FromArgb(255, pixelData[2], pixelData[1], pixelData[0]);
            
            // Update color preview in UI
            ColorPreviewBrush.Color = _selectedColorToRemove;
            
            _isPickingColor = false;
            this.Cursor = Cursors.Arrow;
            UpdateStatus($"Selected color: RGB({_selectedColorToRemove.R}, {_selectedColorToRemove.G}, {_selectedColorToRemove.B}) - Adjust tolerance and click 'Remove'");
        }
        catch (Exception ex)
        {
            _isPickingColor = false;
            this.Cursor = Cursors.Arrow;
            UpdateStatus($"Error picking color: {ex.Message}");
        }
    }
    
    private void SliderTolerance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtToleranceValue != null)
        {
            TxtToleranceValue.Text = ((int)e.NewValue).ToString();
        }
    }
    
    private void CanvasEdited_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentImage == null) return;
        
        Point currentPoint = e.GetPosition(CanvasEdited);
        
        if (_isCropMode && e.RightButton == MouseButtonState.Pressed && CanvasEdited.IsMouseCaptured)
        {
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
            
            UpdateCropInputFields();
        }
        else if (_isMovingCrop && e.LeftButton == MouseButtonState.Pressed && CanvasEdited.IsMouseCaptured)
        {
            double deltaX = currentPoint.X - _cropMoveStartPoint.X;
            double deltaY = currentPoint.Y - _cropMoveStartPoint.Y;
            
            double currentX = Canvas.GetLeft(RectCropSelection);
            double currentY = Canvas.GetTop(RectCropSelection);
            double newX = currentX + deltaX;
            double newY = currentY + deltaY;
            
            newX = Math.Max(0, Math.Min(newX, CanvasEdited.Width - RectCropSelection.Width));
            newY = Math.Max(0, Math.Min(newY, CanvasEdited.Height - RectCropSelection.Height));
            
            Canvas.SetLeft(RectCropSelection, newX);
            Canvas.SetTop(RectCropSelection, newY);
            
            _cropMoveStartPoint = currentPoint;
        }
    }
    
    private void CanvasEdited_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentImage == null) return;
        
        if (_isCropMode || _isMovingCrop)
        {
            CanvasEdited.ReleaseMouseCapture();
            _isCropMode = false;
            _isMovingCrop = false;
            UpdateCropInputFields();
        }
    }
    
    private void UpdateCropInputFields()
    {
        if (_currentImage == null || RectCropSelection.Visibility != Visibility.Visible) return;
        
        double imageAspectRatio = (double)_currentImage.PixelWidth / _currentImage.PixelHeight;
        double canvasAspectRatio = CanvasEdited.Width / CanvasEdited.Height;
        
        double displayedWidth, displayedHeight;
        if (imageAspectRatio > canvasAspectRatio)
        {
            displayedWidth = CanvasEdited.Width;
            displayedHeight = CanvasEdited.Width / imageAspectRatio;
        }
        else
        {
            displayedHeight = CanvasEdited.Height;
            displayedWidth = CanvasEdited.Height * imageAspectRatio;
        }
        
        double scaleX = _currentImage.PixelWidth / displayedWidth;
        double scaleY = _currentImage.PixelHeight / displayedHeight;
        
        double canvasWidth = RectCropSelection.Width;
        double canvasHeight = RectCropSelection.Height;
        
        int width = (int)(canvasWidth * scaleX);
        int height = (int)(canvasHeight * scaleY);
        
        if (TxtCropWidth != null && TxtCropHeight != null)
        {
            TxtCropWidth.Text = width.ToString();
            TxtCropHeight.Text = height.ToString();
        }
    }
    
    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (RectCropSelection.Visibility != Visibility.Visible || _currentImage == null) return;
        
        int moveAmount = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
        
        switch (e.Key)
        {
            case Key.Up:
                MoveCropSelection(0, -moveAmount);
                e.Handled = true;
                break;
            case Key.Down:
                MoveCropSelection(0, moveAmount);
                e.Handled = true;
                break;
            case Key.Left:
                MoveCropSelection(-moveAmount, 0);
                e.Handled = true;
                break;
            case Key.Right:
                MoveCropSelection(moveAmount, 0);
                e.Handled = true;
                break;
        }
    }
    
    private void BtnCrop_Click(object sender, RoutedEventArgs e)
    {
        if (_currentImage == null) return;
        
        if (RectCropSelection.Visibility != Visibility.Visible || 
            RectCropSelection.Width <= 0 || RectCropSelection.Height <= 0)
        {
            MessageBox.Show("Please select an area to crop by right-clicking and dragging on the image.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        ApplyCrop();
    }
    
    private void ApplyCrop()
    {
        if (_currentImage == null) return;
        
        SaveToUndoStack();
        
        double imageAspectRatio = (double)_currentImage.PixelWidth / _currentImage.PixelHeight;
        double canvasAspectRatio = CanvasEdited.Width / CanvasEdited.Height;
        
        double displayedWidth, displayedHeight;
        if (imageAspectRatio > canvasAspectRatio)
        {
            displayedWidth = CanvasEdited.Width;
            displayedHeight = CanvasEdited.Width / imageAspectRatio;
        }
        else
        {
            displayedHeight = CanvasEdited.Height;
            displayedWidth = CanvasEdited.Height * imageAspectRatio;
        }
        
        double scaleX = _currentImage.PixelWidth / displayedWidth;
        double scaleY = _currentImage.PixelHeight / displayedHeight;
        
        double canvasX = Canvas.GetLeft(RectCropSelection);
        double canvasY = Canvas.GetTop(RectCropSelection);
        double canvasWidth = RectCropSelection.Width;
        double canvasHeight = RectCropSelection.Height;
        
        double offsetX = (CanvasEdited.Width - displayedWidth) / 2;
        double offsetY = (CanvasEdited.Height - displayedHeight) / 2;
        
        int x = (int)((canvasX - offsetX) * scaleX);
        int y = (int)((canvasY - offsetY) * scaleY);
        int width = (int)(canvasWidth * scaleX);
        int height = (int)(canvasHeight * scaleY);
        
        x = Math.Max(0, Math.Min(x, _currentImage.PixelWidth - 1));
        y = Math.Max(0, Math.Min(y, _currentImage.PixelHeight - 1));
        width = Math.Min(width, _currentImage.PixelWidth - x);
        height = Math.Min(height, _currentImage.PixelHeight - y);
        
        if (width <= 0 || height <= 0)
        {
            MessageBox.Show("Invalid crop selection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        CroppedBitmap cropped = new(_currentImage, new Int32Rect(x, y, width, height));
        BitmapImage result = ConvertBitmapSourceToBitmapImage(cropped);
        result.Freeze();
        
        _currentImage = result;
        _isCropMode = false;
        RectCropSelection.Visibility = Visibility.Collapsed;
        UpdateImageDisplay();
        ClearRedoStack();
        UpdateStatus($"Cropped to {width}x{height}");
    }
    
    private void SetCropSizeFromInput()
    {
        if (_currentImage == null) return;
        
        if (!int.TryParse(TxtCropWidth.Text, out int width) || width <= 0) return;
        if (!int.TryParse(TxtCropHeight.Text, out int height) || height <= 0) return;
        
        double imageAspectRatio = (double)_currentImage.PixelWidth / _currentImage.PixelHeight;
        double canvasAspectRatio = CanvasEdited.Width / CanvasEdited.Height;
        
        double displayedWidth, displayedHeight;
        if (imageAspectRatio > canvasAspectRatio)
        {
            displayedWidth = CanvasEdited.Width;
            displayedHeight = CanvasEdited.Width / imageAspectRatio;
        }
        else
        {
            displayedHeight = CanvasEdited.Height;
            displayedWidth = CanvasEdited.Height * imageAspectRatio;
        }
        
        double scaleX = displayedWidth / _currentImage.PixelWidth;
        double scaleY = displayedHeight / _currentImage.PixelHeight;
        
        double cropWidth = width * scaleX;
        double cropHeight = height * scaleY;
        
        double offsetX = (CanvasEdited.Width - displayedWidth) / 2;
        double offsetY = (CanvasEdited.Height - displayedHeight) / 2;
        
        double cropX, cropY;
        if (RectCropSelection.Visibility == Visibility.Visible)
        {
            cropX = Canvas.GetLeft(RectCropSelection);
            cropY = Canvas.GetTop(RectCropSelection);
        }
        else
        {
            cropX = offsetX;
            cropY = offsetY;
        }
        
        cropX = Math.Max(offsetX, Math.Min(cropX, offsetX + displayedWidth - cropWidth));
        cropY = Math.Max(offsetY, Math.Min(cropY, offsetY + displayedHeight - cropHeight));
        
        RectCropSelection.Width = cropWidth;
        RectCropSelection.Height = cropHeight;
        Canvas.SetLeft(RectCropSelection, cropX);
        Canvas.SetTop(RectCropSelection, cropY);
        RectCropSelection.Visibility = Visibility.Visible;
    }
    
    private void TxtCropWidth_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SetCropSizeFromInput();
            e.Handled = true;
        }
    }
    
    private void TxtCropHeight_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SetCropSizeFromInput();
            e.Handled = true;
        }
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
        
        newX = Math.Max(0, Math.Min(newX, CanvasEdited.Width - RectCropSelection.Width));
        newY = Math.Max(0, Math.Min(newY, CanvasEdited.Height - RectCropSelection.Height));
        
        Canvas.SetLeft(RectCropSelection, newX);
        Canvas.SetTop(RectCropSelection, newY);
    }
    
    private void BtnCropMoveUp_Click(object sender, RoutedEventArgs e) => MoveCropSelection(0, -10);
    private void BtnCropMoveDown_Click(object sender, RoutedEventArgs e) => MoveCropSelection(0, 10);
    private void BtnCropMoveLeft_Click(object sender, RoutedEventArgs e) => MoveCropSelection(-10, 0);
    private void BtnCropMoveRight_Click(object sender, RoutedEventArgs e) => MoveCropSelection(10, 0);
    
    private void BtnCropCenter_Click(object sender, RoutedEventArgs e)
    {
        if (RectCropSelection.Visibility != Visibility.Visible || _currentImage == null) return;
        
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
            Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|BMP Image|*.bmp|GIF Image|*.gif|TIFF Image|*.tiff;*.tif|All Files|*.*",
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
    // Help and About Functions
    // ============================================
    private void BtnHelp_Click(object sender, RoutedEventArgs e)
    {
        string helpText = @"PictureWorks v2.0.0 - Help

HOW TO USE:

1. OPEN IMAGE:
   - Drag and drop an image onto the window
   - Click 'Open' button
   - Ctrl+V to paste from clipboard

2. RESIZE:
   - Select 'Percent' or 'Pixel' mode
   - Enter the desired size
   - Toggle 'Maintain Aspect' on/off
   - Click 'Apply Resize'

3. ROTATE & FLIP:
   - ↺/↻ buttons rotate 90° left/right
   - ↔/↕ buttons flip horizontal/vertical

4. CROP:
   - Select aspect ratio preset (Free, 16:9, 4:3, 1:1, etc.)
   - Right-click and drag to select crop area
   - Left-click and drag to move selection
   - Arrow keys for fine-tuning (Shift for 10px)
   - Click 'Apply Crop' to apply

5. BRIGHTNESS/CONTRAST:
   - Adjust sliders (-100 to +100)
   - Click 'Apply' to apply changes
   - Click 'Reset' to reset sliders

6. FILTERS:
   - 'Grayscale' - Convert to black & white
   - 'Sepia' - Apply vintage sepia tone

7. REMOVE BACKGROUND:
   - Click 'Pick Color' then click on image
   - Adjust tolerance slider (0-100)
   - Click 'Remove' to make color transparent

8. SAVE:
   - Click 'Save As' to save
   - Supports PNG, JPG, BMP, GIF, TIFF
   - Ctrl+C to copy to clipboard

KEYBOARD SHORTCUTS:
- Ctrl+V: Paste image from clipboard
- Ctrl+C: Copy image to clipboard
- Arrow Keys: Move crop selection (1px)
- Shift + Arrow Keys: Move crop (10px)
- Mouse Wheel: Zoom in/out";

        MessageBox.Show(helpText, "PictureWorks - Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        AboutWindow aboutWindow = new() { Owner = this };
        aboutWindow.ShowDialog();
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
