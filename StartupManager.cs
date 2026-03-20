using Microsoft.Win32;
using System.Windows.Forms;

namespace WindowsMMBClip;

internal static class StartupManager
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WindowsMMBClip";

    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            if (key == null) return;

            if (enable)
            {
                key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch { }
    }
}
