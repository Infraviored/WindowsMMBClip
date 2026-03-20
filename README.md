# WindowsMMBClip

**Linux-style Primary Selection (Middle Mouse Button Paste) for Windows.**

WindowsMMBClip brings the efficient "Primary Selection" behavior from Linux (Ubuntu/X11) to Windows. It maintains a separate selection buffer that can be pasted instantly with a middle mouse click, without overwriting your standard `Ctrl+C` clipboard.

## 🚀 Features

- **True Primary Selection**: Selecting text automatically populates the primary buffer.
- **MMB Paste**: Paste the primary buffer instantly with the Middle Mouse Button.
- **Context Aware**: Smart enough to know when you are clicking a browser tab (closes it) vs. a text field (pastes).
- **Clipboard Separation**: Your standard `Ctrl+C` / `Ctrl+V` history remains untouched.
- **Performance Tuned**: High-speed spatial caching and optimized OLE clipboard manipulation.
- **DPI Aware**: Modern, scaling UI for high-resolution displays.

## 🛠 Installation

WindowsMMBClip is a **Single-File Portable** application. No installation is required.

### **Option 1: Automatic Startup (Recommended)**
1. Run `WindowsMMBClip.exe`.
2. Right-click the tray icon and select **Settings...**.
3. Check **Start WindowsMMBClip with Windows**.
4. Click **Close**.

### **Option 2: Manual Installation (The "Old School" way)**
1. Press `Win + R` on your keyboard.
2. Type `shell:startup` and press Enter.
3. Copy `WindowsMMBClip.exe` (or a shortcut to it) into the folder that opens.
4. The application will now launch every time you log into Windows.

## ⚙️ Configuration

If you find that the application pastes the wrong text in slow environments (like Remote Desktop or heavy IDEs), you can tune the **Paste Reliability** delay in the Settings menu.

## 🏗 Building from Source

Requires **.NET 8 SDK**.

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## 📜 License
MIT
