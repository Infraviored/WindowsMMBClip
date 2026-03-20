using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsMMBClip;

internal sealed class GlobalMouseHook : IDisposable
{
    private NativeMethods.HookProc? _hookProc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private Thread? _hookThread;
    private uint _hookThreadId;
    private readonly ManualResetEventSlim _hookStarted = new(false);

    public Func<int, int, bool>? CanHandleMiddleButton { get; set; }
    public Action? MiddleButtonPressed { get; set; }

    public void Start()
    {
        if (_hookThread != null)
        {
            return;
        }

        _hookThread = new Thread(HookThreadMain)
        {
            IsBackground = true,
            Name = "WindowsMMBClip.MouseHook"
        };
        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.Start();
        _hookStarted.Wait();
    }

    private void HookThreadMain()
    {
        _hookProc = HookCallback;
        _hookThreadId = NativeMethods.GetCurrentThreadId();
        IntPtr moduleHandle = NativeMethods.GetModuleHandle(null);

        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _hookProc,
            moduleHandle,
            0);
        if (_hookHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        _hookStarted.Set();

        try
        {
            while (NativeMethods.GetMessage(out NativeMethods.MSG msg, IntPtr.Zero, 0, 0) > 0)
            {
                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }
        }
        finally
        {
            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_MBUTTONDOWN)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            if (CanHandleMiddleButton?.Invoke(hookStruct.pt.x, hookStruct.pt.y) == true)
            {
                MiddleButtonPressed?.Invoke();
                return (IntPtr)1;
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookThreadId != 0)
        {
            NativeMethods.PostThreadMessage(_hookThreadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        if (_hookThread != null)
        {
            _hookThread.Join(TimeSpan.FromSeconds(2));
            _hookThread = null;
        }

        _hookThreadId = 0;
        _hookProc = null;
        _hookStarted.Dispose();
    }
}
