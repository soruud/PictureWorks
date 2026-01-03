# PictureWorks

A Windows desktop application for image editing - resize, crop, rotate, and convert images to various formats.

## Features

- **Open Images**: Drag & drop or use File → Open menu
- **Resize**: Resize by percentage or pixel size, with optional aspect ratio preservation
- **Crop**: 
  - Right-click and drag to select crop area
  - Left-click and drag to move existing crop selection
  - Use arrow keys to fine-tune crop position (Shift+Arrow for 10px steps)
  - Enter crop dimensions manually and press Enter
  - Crop selection anchored to top-left corner
- **Rotate**: Rotate in 90° steps (left/right) using rotation icons (↺ ↻)
- **Zoom**: Mouse wheel zoom in/out (0.1x steps, range 0.1x - 5.0x)
- **Save As**: Save images in multiple formats (PNG, JPG, BMP, GIF, TIFF, WEBP)
- **Undo/Redo**: Full undo/redo support for all operations (up to 20 steps)
- **Dark Mode**: Toggle between light and dark themes
- **Modern UI**: Clean, modern interface with custom color scheme (not Windows gray)
- **Image Anchoring**: Image always anchored to top-left corner for consistent positioning

## Supported Formats

- PNG
- JPG/JPEG
- BMP
- GIF
- TIFF
- WEBP

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (included in self-contained executable)

## Installation

1. Download `PictureWorks_v1.2.0.exe` (or latest version) from the `Ready Builds` folder
2. Run the executable - no installation required (self-contained build)

## Usage

1. **Open an image**: Drag and drop an image onto the window, or click "Open" button
2. **Resize**: 
   - Select "Percent" or "Pixel" mode
   - Enter the desired size
   - Toggle "Maintain Aspect" on/off
   - Click "Apply Resize"
3. **Crop**: 
   - **Right-click and drag** on the image to select a crop area
   - **Left-click and drag** to move the existing crop selection
   - Use **arrow keys** to fine-tune position (1px per press, Shift+Arrow for 10px)
   - Enter width/height in input fields and press **Enter** to set crop size
   - Click "Apply Crop" to apply the crop
4. **Rotate**: 
   - Click ↺ (left) or ↻ (right) rotation icons to rotate 90° at a time
5. **Zoom**: 
   - Use **mouse wheel** to zoom in/out
   - Image stays anchored to top-left corner
6. **Save**: 
   - Click "Save As" button
   - Choose format and location
   - Click Save

## Keyboard Shortcuts

- **Arrow Keys**: Move crop selection (1px)
- **Shift + Arrow Keys**: Move crop selection (10px)
- **Enter** (in crop input fields): Apply crop size from input fields

## Version History

### v1.2.0 (Current)
- Added splash screen with logo (displays for 4 seconds on startup)
- Fixed duplicate resource entries in project file
- Improved application icon handling

### v1.1.9
- Updated PayPal donation URL to correct link

### v1.1.8
- Fixed About window - increased size, larger Close button, smaller font, better spacing

### v1.1.7
- Fixed Close button position - removed invalid RowDefinition element, added spacer row

### v1.1.6
- Increased About window height for better Close button spacing

### v1.1.5
- Fixed AboutWindow StaticResource error - defined resources locally

### v1.1.4
- Added Help and About buttons with pop-up windows
- Added PayPal donation link in About window

### v1.1.3
- Added Help and About buttons

### v1.1.2
- Fixed image anchoring to top-left corner for zoom, resize, and crop operations
- Image no longer shifts position during operations

### v1.1.1
- Fixed mouse wheel zoom - now zooms instead of scrolling

### v1.1.0
- Fixed button sizes and symbols (crop buttons now same size as rotate buttons)
- Added rotation icons (↺ ↻) instead of arrows
- Implemented mouse wheel zoom functionality

### v1.0.9
- Removed "Set Size" button
- Crop size updates on Enter key press
- Crop created in top-left corner if missing
- Fixed arrow symbols for crop buttons

### v1.0.8
- Single image view with proper scaling
- Improved resize quality using RenderTargetBitmap
- Enhanced crop functionality with keyboard controls
- Real-time crop input field updates

### v1.0.7
- Fixed version numbering (corrected from 1.1.0)
- Image scaling improvements
- Crop UI enhancements

### v1.0.6
- Fixed null reference exceptions
- Improved error handling
- Self-contained build support

### v1.0.5 and earlier
- Initial releases with basic functionality

## Current Version

**v1.2.0** - Latest stable release

## License

MIT License - Free use and modification
