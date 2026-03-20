using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindowsMMBClip;

internal static class IconGenerator
{
    public static Icon Generate()
    {
        // Colors from SVG
        var winBlue = ColorTranslator.FromHtml("#0078D4");
        var mouseGray = ColorTranslator.FromHtml("#2D3748");
        var primaryOrange = ColorTranslator.FromHtml("#E95420");
        var lightBlue = ColorTranslator.FromHtml("#80BCE8");

        using var bitmap = new Bitmap(256, 256);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // 1. Back Clipboard Sheet
            using (var p = new Pen(winBlue, 12))
            {
                g.DrawRoundedRectangle(p, 30, 30, 130, 160, 15);
            }

            // 2. Front Clipboard Sheet (White)
            g.FillRoundedRectangle(Brushes.White, 70, 70, 130, 160, 15);
            using (var p = new Pen(winBlue, 12))
            {
                g.DrawRoundedRectangle(p, 70, 70, 130, 160, 15);
            }

            // 3. Document lines
            using (var p = new Pen(lightBlue, 10))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, 95, 110, 135, 110);
                g.DrawLine(p, 95, 145, 125, 145);
                g.DrawLine(p, 95, 180, 140, 180);
            }

            // 4. Mouse Body
            using (var p = new Pen(mouseGray, 12))
            {
                g.FillRoundedRectangle(Brushes.White, 140, 80, 100, 165, 45);
                g.DrawRoundedRectangle(p, 140, 80, 100, 165, 45);
                
                // Middle divider
                g.DrawLine(p, 190, 80, 190, 145);
                
                // Horizontal divider
                g.DrawArc(p, 140, 130, 100, 30, 0, 180);
            }

            // 5. Orange Middle Button (The "Primary" indicator)
            g.FillRoundedRectangle(new SolidBrush(primaryOrange), 180, 105, 20, 35, 8);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath GetRoundedRectPath(int x, int y, int width, int height, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
