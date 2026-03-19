using System.Threading;
using System.Windows.Forms;

namespace UbuntuPrimaryClipboard;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, "UbuntuPrimaryClipboard.Singleton", out bool isNewInstance);
        if (!isNewInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}
