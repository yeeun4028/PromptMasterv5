using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;

namespace PromptMasterv5.Infrastructure.Helpers
{
    public static class ScreenCaptureHelper
    {
        public static Bitmap CaptureFullScreen()
        {
             // Get virtual screen bounds (all monitors)
            int left = (int)SystemParameters.VirtualScreenLeft;
            int top = (int)SystemParameters.VirtualScreenTop;
            int width = (int)SystemParameters.VirtualScreenWidth;
            int height = (int)SystemParameters.VirtualScreenHeight;

            // Handle High DPI scaling if needed, but VirtualScreen usually returns logical units.
            // System.Drawing (GDI+) works in pixels. SystemParameters works in logical units (DPI aware).
            // This mismatch causes the "Zoomed In" screenshot issue if not handled.
            // However, the original code in ScreenCaptureOverlay.xaml.cs used SystemParameters directly 
            // and it seemed to rely on the overlay being full screen.
            
            // Actually, for GDI+ CopyFromScreen, we want physical pixels.
            // SystemParameters.VirtualScreen* are LOGICAL units.
            // Screen.AllScreens provides pixel bounds.
            
            // Let's use System.Windows.Forms.Screen to get physical bounds which is more robust for GDI+.
            
            int minX = 0, minY = 0, maxX = 0, maxY = 0;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.X < minX) minX = screen.Bounds.X;
                if (screen.Bounds.Y < minY) minY = screen.Bounds.Y;
                if (screen.Bounds.Right > maxX) maxX = screen.Bounds.Right;
                if (screen.Bounds.Bottom > maxY) maxY = screen.Bounds.Bottom;
            }
            
            width = maxX - minX;
            height = maxY - minY;

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(minX, minY, 0, 0, new System.Drawing.Size(width, height));
            }
            return bmp;
        }
    }
}
