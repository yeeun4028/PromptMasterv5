using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Views;
using System.Windows;
using Application = System.Windows.Application;
using PromptMasterv5.Infrastructure.Helpers;
using System;
using System.Threading.Tasks;

namespace PromptMasterv5.Infrastructure.Services
{
    public class WindowManager : IWindowManager
    {
        public async Task<byte[]?> ShowCaptureWindowAsync(Func<byte[], System.Windows.Rect, Task>? onCaptureProcessing = null)
        {
            // 0. Cleanup: Close any existing TranslationPopup windows to prevent "ghost windows" in screenshot
            bool anyClosed = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var existingPopups = new System.Collections.Generic.List<Window>();
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is TranslationPopup)
                    {
                        existingPopups.Add(win);
                    }
                }

                foreach (var popup in existingPopups)
                {
                    try
                    {
                        if (popup.IsLoaded)
                        {
                            popup.Close();
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore race conditions where window is already closing
                    }
                }
                
                return existingPopups.Count > 0;
            });

            if (anyClosed)
            {
                // Give UI time to repaint background (remove the closed window from screen buffer)
                await Task.Delay(150);
            }

            // 1. Capture screen cleanly (after cleanup)
            // Note: Use a helper that uses System.Drawing so we don't block UI thread logic excessively
            using var screenBmp = Helpers.ScreenCaptureHelper.CaptureFullScreen();

            // 2. Show Overlay on UI thread
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var capture = new ScreenCaptureOverlay(screenBmp, onCaptureProcessing);
                if (capture.ShowDialog() == true)
                {
                    return capture.CapturedImageBytes;
                }
                return null;
            });
        }

        public void ShowTranslationPopup(string text, System.Windows.Rect? placementTarget = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var popup = new TranslationPopup(text);
                if (placementTarget.HasValue)
                {
                    popup.SetPlacementTarget(placementTarget.Value);
                    
                    // Basic boundary check to ensure it doesn't spawn partially off-screen
                    // (Optional, but good UX. We can rely on user dragging if needed for now)
                }
                popup.Show();
                popup.Activate();
            });
        }
    }
}
