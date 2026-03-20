using System.Threading;
using System.Windows.Forms;

namespace WindowsMMBClip;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly SynchronizationContext _uiContext;
    private readonly NotifyIcon _trayIcon;
    private readonly ClipboardListenerWindow _clipboardWindow;
    private readonly SelectionTracker _selectionTracker;
    private readonly GlobalMouseHook _mouseHook;
    private readonly PrimaryClipboardService _primaryService;
    private readonly AppSettings _settings;
    private ToolStripMenuItem _pauseItem = null!;

    public TrayAppContext()
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = AppSettings.Load();
        _primaryService = new PrimaryClipboardService(_settings);
        _clipboardWindow = new ClipboardListenerWindow();
        _selectionTracker = new SelectionTracker();
        _mouseHook = new GlobalMouseHook();

        _clipboardWindow.ClipboardUpdated += (_, sequence) => _primaryService.OnClipboardUpdated(sequence);
        _selectionTracker.PrimaryTextDetected += (_, text) => _primaryService.UpdatePrimary(text);
        _mouseHook.CanHandleMiddleButton = (x, y) => 
        {
            if (_primaryService.IsPaused || !_primaryService.CanPastePrimary) return false;
            return TargetAnalyzer.IsEditableElementAtPoint(x, y);
        };
        _mouseHook.MiddleButtonPressed = () => _uiContext.Post(_ => _ = _primaryService.PastePrimaryAsync(), null);

        _trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            Text = "Windows MMB Clip",
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

        menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        _pauseItem = new ToolStripMenuItem("Pause capture");
        _pauseItem.Click += (_, _) => TogglePause();
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        return menu;
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        form.ShowDialog();
    }

    private void TogglePause()
    {
        _primaryService.IsPaused = !_primaryService.IsPaused;
        _pauseItem.Checked = _primaryService.IsPaused;
        _pauseItem.Text = _primaryService.IsPaused ? "Resume capture" : "Pause capture";
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
