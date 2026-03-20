using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsMMBClip;

internal sealed class PrimaryClipboardService
{
    private readonly SemaphoreSlim _pasteGate = new(1, 1);
    private readonly AppSettings _settings;
    private bool _bridgeActive;
    private uint _lastMirroredClipboardSequence;

    public string? PrimaryText { get; private set; }
    public bool IsPaused { get; set; }

    public PrimaryClipboardService(AppSettings settings)
    {
        _settings = settings;
    }

    public bool CanPastePrimary => !string.IsNullOrEmpty(PrimaryText);

    public void UpdatePrimary(string text)
    {
        if (IsPaused) return;

        string normalized = NormalizeText(text);
        if (string.IsNullOrEmpty(normalized)) return;

        PrimaryText = normalized;
    }

    public void OnClipboardUpdated(uint sequence)
    {
        if (_bridgeActive || sequence == 0 || sequence == _lastMirroredClipboardSequence) return;
        RefreshSystemClipboardMirror(sequence);
    }

    public void RefreshSystemClipboardMirror(uint? observedSequence = null)
    {
        if (_bridgeActive) return;

        try
        {
            _lastMirroredClipboardSequence = observedSequence ?? NativeMethods.GetClipboardSequenceNumber();
        }
        catch (ExternalException) { }
    }

    public async Task PastePrimaryAsync()
    {
        if (IsPaused || string.IsNullOrEmpty(PrimaryText)) return;

        await _pasteGate.WaitAsync().ConfigureAwait(true);

        try
        {
            if (IsPaused || string.IsNullOrEmpty(PrimaryText)) return;

            IDataObject? backupDataObject = null;

            try
            {
                IDataObject? current = RetryClipboard(() => Clipboard.GetDataObject());
                if (current != null)
                {
                    backupDataObject = SnapshotDataObject(current);
                }
            }
            catch (ExternalException) { return; }

            _bridgeActive = true;

            try
            {
                string primary = PrimaryText!;
                var primaryData = new DataObject();
                primaryData.SetText(primary, TextDataFormat.UnicodeText);
                primaryData.SetText(primary, TextDataFormat.Text);
                ExcludeFromHistory(primaryData);

                RetryClipboard(() => Clipboard.SetDataObject(primaryData, false));
                NativeMethods.SendCtrlV();
                
                // Use user-defined delays from settings
                await Task.Delay(_settings.PasteDelay).ConfigureAwait(true);

                if (backupDataObject != null)
                {
                    RetryClipboard(() => Clipboard.SetDataObject(backupDataObject, true));
                }

                await Task.Delay(_settings.StabilizationDelay).ConfigureAwait(true);
            }
            finally
            {
                _bridgeActive = false;
                RefreshSystemClipboardMirror(NativeMethods.GetClipboardSequenceNumber());
            }
        }
        finally
        {
            _pasteGate.Release();
        }
    }

    private static DataObject SnapshotDataObject(IDataObject source)
    {
        var target = new DataObject();
        foreach (string format in source.GetFormats(false))
        {
            try
            {
                object? data = source.GetData(format, false);
                if (data != null) target.SetData(format, data);
            }
            catch { }
        }
        return target;
    }

    private static void ExcludeFromHistory(DataObject data)
    {
        byte[] no = { 0, 0, 0, 0 };
        data.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(no));
        data.SetData("CanIncludeInClipboardHistory", new MemoryStream(no));
        data.SetData("CanUploadToCloudClipboard", new MemoryStream(no));
    }

    private static string NormalizeText(string text) => text.Replace("\0", string.Empty);

    private static void RetryClipboard(Action action, int attempts = 10, int delayMs = 15)
    {
        RetryClipboard<object?>(() => { action(); return null; }, attempts, delayMs);
    }

    private static T RetryClipboard<T>(Func<T> action, int attempts = 10, int delayMs = 15)
    {
        ExternalException? last = null;
        for (int i = 0; i < attempts; i++)
        {
            try { return action(); }
            catch (ExternalException ex) { last = ex; Thread.Sleep(delayMs); }
        }
        throw last ?? new ExternalException("Clipboard access failed.");
    }
}
