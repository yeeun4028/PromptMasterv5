using System.Windows;
using System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace PromptMasterv5.Infrastructure.Services
{
    public class WindowPositionService
    {
        public void PositionWindowNearMouse(Window window, double offset = 15)
        {
            var mouse = Cursor.Position;
            var screen = Screen.FromPoint(mouse);
            var workArea = screen.WorkingArea;

            // DPI handling if needed (assuming WPF handles logical pixels correctly)
            var presentationSource = PresentationSource.FromVisual(window);
            double dpiX = 1.0, dpiY = 1.0;
            if (presentationSource != null && presentationSource.CompositionTarget != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }

            double targetLeft = mouse.X / dpiX;
            double targetTop = (mouse.Y / dpiY) + offset;

            // Ensure window stays within screen bounds
            if (targetLeft + window.Width > workArea.Right / dpiX)
            {
                targetLeft = (workArea.Right / dpiX) - window.Width - offset;
            }
            if (targetLeft < workArea.Left / dpiX)
            {
                targetLeft = (workArea.Left / dpiX) + offset;
            }

            if (targetTop + window.Height > workArea.Bottom / dpiY)
            {
                // If it goes below screen, show above mouse
                targetTop = (mouse.Y / dpiY) - window.Height - offset;
            }

            window.Left = targetLeft;
            window.Top = targetTop;
        }
    }
}
