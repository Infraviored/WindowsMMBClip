using System.Threading;
using System.Windows.Forms;

namespace UbuntuPrimaryClipboard;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly SynchronizationContext _uiContext;
    private readonly NotifyIcon _trayIcon;
    private readonly ClipboardListenerWindow _clipboardWindow;
    private readonly SelectionTracker _selectionTracker;
    private readonly GlobalMouseHook _mouseHook;
    private readonly PrimaryClipboardService _primaryService;
    private ToolStripMenuItem _pauseItem = null!;

    public TrayAppContext()
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _primaryService = new PrimaryClipboardService();
        _clipboardWindow = new ClipboardListenerWindow();
        _selectionTracker = new SelectionTracker();
        _mouseHook = new GlobalMouseHook();

        _clipboardWindow.ClipboardUpdated += (_, sequence) => _primaryService.OnClipboardUpdated(sequence);
        _selectionTracker.PrimaryTextDetected += (_, text) => _primaryService.UpdatePrimary(text);
        _mouseHook.CanHandleMiddleButton = () => _primaryService.CanPastePrimary && !_primaryService.IsPaused;
        _mouseHook.MiddleButtonPressed = () => _uiContext.Post(_ => _ = _primaryService.PastePrimaryAsync(), null);

        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Ubuntu Primary Clipboard",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _selectionTracker.Start();
        _mouseHook.Start();
        _primaryService.RefreshSystemClipboardMirror(NativeMethods.GetClipboardSequenceNumber());
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show Buffers", null, (_, _) => ShowBuffers());
        _pauseItem = new ToolStripMenuItem("Pause capture");
        _pauseItem.Click += (_, _) => TogglePause();
        menu.Items.Add(_pauseItem);
        menu.Items.Add("Clear Primary", null, (_, _) => _primaryService.ClearPrimary());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void TogglePause()
    {
        _primaryService.IsPaused = !_primaryService.IsPaused;
        _pauseItem.Checked = _primaryService.IsPaused;
        _pauseItem.Text = _primaryService.IsPaused ? "Resume capture" : "Pause capture";
    }

    private void ShowBuffers()
    {
        string primary = Truncate(_primaryService.PrimaryText);
        string systemClipboard = Truncate(_primaryService.SystemClipboardText);

        MessageBox.Show(
            $"Primary buffer (middle-click):\r\n{primary}\r\n\r\nSystem clipboard (Ctrl+C / Ctrl+V):\r\n{systemClipboard}",
            $"Ubuntu Primary Clipboard{(_primaryService.IsPaused ? " [Paused]" : string.Empty)}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static string Truncate(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "<empty>";
        return text.Length <= 2000 ? text : text[..2000] + "\r\n...[truncated]";
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _mouseHook.Dispose();
        _selectionTracker.Dispose();
        _clipboardWindow.Dispose();
        base.ExitThreadCore();
    }
}
