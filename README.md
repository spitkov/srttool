# 🎬 SrtTool

A professional-grade .srt subtitle editor built with C# Windows Forms. Designed for efficiency, ease of use, and powerful subtitle manipulation capabilities.

## ✨ Features

### 📝 Basic Operations
- Open and save SRT files with UTF-8 encoding
- Edit subtitle text, start times, and end times
- Drag selection for multiple subtitle entries
- Right-click context menu for quick access to all operations
- Undo/Redo support for all operations
- File association for .srt files

### ⏱️ Timing Operations
- 🔄 Shift timestamps (forward/backward)
- 📊 Scale timing (stretch/compress)
- 🎯 Snap to intervals
- 🎥 Sync to frame rate (23.976, 25, 29.97 fps)
- 🔍 Smart duration adjustment based on text length
- ⚡ Fix timing overlaps automatically
- 🌟 Adjust minimum display time
- 🔧 Fix gaps between subtitles

### 📋 Text Operations
- 🔠 Convert case (UPPERCASE, lowercase, Title Case)
- 🧹 Remove formatting tags
- 🔍 Fix common OCR errors
- 🔄 Convert text encoding
- 🎭 Remove hearing impaired text
- 📏 Split long lines
- 🔗 Merge short lines

### 📦 Batch Operations
- 🔄 Merge multiple SRT files
- ✂️ Split subtitles at selection
- 🔢 Reindex subtitles
- 🔧 Fix timing gaps

### 🔄 Format Conversion
#### Import from:
- SubViewer (.sub)
- MicroDVD (.sub)
- SAMI (.smi)
- SSA/ASS (.ssa/.ass)
- WebVTT (.vtt)

#### Export to:
- SubViewer (.sub)
- MicroDVD (.sub)
- SAMI (.smi)
- SSA/ASS (.ssa/.ass)
- WebVTT (.vtt)

## 🎨 Design

### User Interface
- Clean and intuitive Windows Forms interface
- Professional menu system with all operations
- Context-sensitive right-click menus
- Multi-select support with drag selection
- Grid-based subtitle list with sortable columns
- Real-time preview of changes

### Technical Design
- 🏗️ **Architecture**:
  - Clean separation of UI and business logic
  - Event-driven design for user interactions
  - Modular code structure for easy maintenance

- 💾 **Data Management**:
  - Efficient in-memory subtitle storage
  - Robust file I/O handling
  - Full undo/redo stack implementation

- 🔒 **Error Handling**:
  - Comprehensive error checking
  - User-friendly error messages
  - Safe file operations

- 🎯 **Performance**:
  - Fast loading of large subtitle files
  - Efficient batch operations
  - Responsive UI during operations

## 🚀 Getting Started

1. Download the latest release
2. Run the application
3. Open an .srt file or create a new one
4. Use the menu or right-click for operations
5. Save your changes

## ⌨️ Keyboard Shortcuts

- `Ctrl + O` - Open file
- `Ctrl + S` - Save file
- `Ctrl + Z` - Undo
- `Ctrl + Y` - Redo
- `Delete` - Delete selected entries
- `Ctrl + A` - Select all entries
- `F2` - Edit selected entry

## 🛠️ Requirements

- Windows 7 or later
- .NET 6.0 Runtime
- 64-bit operating system

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests.


---
Made with ❤️ using C# and Windows Forms