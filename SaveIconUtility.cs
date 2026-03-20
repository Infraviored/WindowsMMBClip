using System.Drawing;
using System.IO;

namespace WindowsMMBClip;

// Utility to manually regenerate app.ico if the design changes
internal static class SaveIconUtility
{
    public static void Save()
    {
        using var icon = IconGenerator.Generate();
        using var stream = new FileStream("app.ico", FileMode.Create);
        icon.Save(stream);
    }
}
