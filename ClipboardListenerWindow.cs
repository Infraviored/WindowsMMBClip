using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UbuntuPrimaryClipboard;

internal sealed class ClipboardListenerWindow : NativeWindow, IDisposable
{
    public event EventHandler<uint>? ClipboardUpdated;

    public ClipboardListenerWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "UbuntuPrimaryClipboardMessageWindow"
        });

        if (!NativeMethods.AddClipboardFormatListener(Handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            ClipboardUpdated?.Invoke(this, NativeMethods.GetClipboardSequenceNumber());
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            NativeMethods.RemoveClipboardFormatListener(Handle);
            DestroyHandle();
        }
    }
}
