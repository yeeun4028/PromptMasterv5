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
            // 1. Capture screen instantly before any UI thread manipulation or window closing
            // This is the "Static Screen Freeze" approach. It prevents tooltips from hiding when focus changes.
            Bitmap? screenBmp = null;
            try 
            {
                screenBmp = Helpers.ScreenCaptureHelper.CaptureFullScreen();
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"Failed to capture screen instantly: {ex.Message}", "WindowManager");
                return null;
            }

            // 2. Cleanup: Close any existing TranslationPopup windows AFTER capture
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

            // 3. Show Overlay on UI thread using the static background image
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var capture = new ScreenCaptureOverlay(screenBmp, onCaptureProcessing);
                if (capture.ShowDialog() == true)
                {
                    // Memory of screenBmp is managed inside ScreenCaptureOverlay
                    return capture.CapturedImageBytes;
                }
                else
                {
                    screenBmp?.Dispose();
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
