# PictureWorks

A lightweight Windows desktop application for image editing - resize, crop, rotate, flip, adjust colors, apply filters, remove backgrounds, and convert images to various formats.

## Features

### Core Features
- **Open Images**: Drag & drop, File → Open, or Ctrl+V from clipboard
- **Save Images**: Save to PNG, JPG, BMP, GIF, TIFF formats
- **Clipboard Support**: Ctrl+V to paste, Ctrl+C to copy

### Transform
- **Resize**: By percentage or pixel size, with optional aspect ratio lock
- **Crop**: With aspect ratio presets (Free, 16:9, 4:3, 1:1, 3:2, 2:3, 9:16)
- **Rotate**: 90° left/right rotation
- **Flip**: Horizontal and vertical flip
- **Zoom**: Mouse wheel zoom (0.1x - 5.0x)

### Adjustments
- **Brightness**: -100 to +100 adjustment (live preview)
- **Contrast**: -100 to +100 adjustment (live preview)
- **Mouse Wheel Control**: Scroll on sliders to adjust values

### Filters
- **Grayscale**: Convert to black & white (toggle on/off)
- **Sepia**: Apply vintage sepia tone (toggle on/off)

### Background Removal
- **Pick Color**: Click to select color from image
- **Color Preview**: Visual indicator showing selected color
- **Tolerance**: Adjustable 0-100 for color matching
- **Remove**: Make selected color transparent (BGRA32 format)

### Other
- **Undo/Redo**: Up to 20 steps
- **Dark/Light Mode**: Toggle between themes (Dark by default)
- **Splash Screen**: 3.5 second startup screen with logo
- **Modern UI**: Clean interface with two toolbar rows

## Supported Formats

- PNG (with transparency)
- JPG/JPEG
- BMP
- GIF
- TIFF

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (included in self-contained executable)

## Installation

1. Download `PictureWorks_v2.0.4.exe` from the `Ready Builds` folder
2. Run the executable - no installation required (self-contained build)

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+V | Paste image from clipboard |
| Ctrl+C | Copy image to clipboard |
| Arrow Keys | Move crop selection (1px) |
| Shift + Arrow Keys | Move crop selection (10px) |
| Mouse Wheel | Zoom in/out |
| Mouse Wheel on Slider | Adjust slider value |

## Version History

### v2.0.4 (Current) - Stable Release
- **FIXED**: Background removal now works correctly (converts to BGRA32 format)
- **NEW**: Color preview box shows selected color for removal
- **NEW**: Tolerance value display updates live
- **IMPROVED**: Better pixel coordinate calculation for color picking

### v2.0.0 - v2.0.3 - Major Update
- **NEW**: Flip horizontal/vertical
- **NEW**: Brightness/Contrast adjustments with live preview
- **NEW**: Mouse wheel control for sliders
- **NEW**: Grayscale and Sepia filters (toggle on/off)
- **NEW**: Background removal with color picker and tolerance
- **NEW**: Clipboard support (Ctrl+C/V)
- **NEW**: Aspect ratio presets for crop (16:9, 4:3, 1:1, etc.)
- **NEW**: Two-row toolbar layout
- **NEW**: Splash screen (3.5 seconds)
- **NEW**: Application icon
- Dark Mode enabled by default
- Reorganized UI for better workflow

### v1.1.9 - Legacy Stable
- Last version before v2.0 feature additions
- Crop functionality with keyboard controls
- Rotation icons
- Mouse wheel zoom
- Undo/Redo system
- Dark Mode toggle

### v1.0.x - v1.1.x
- Initial releases with basic resize, crop, rotate functionality
- Progressive feature additions and bug fixes

## Current Version

**v2.0.4** - Latest stable release (~70 MB, self-contained)

## License

MIT License - Free use and modification

## Author

Created by Stig Ove K. Ruud  
Email: soruud@gmail.com
