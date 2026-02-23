using System;
using System.Threading.Tasks;

namespace PromptMasterv5.Core.Interfaces
{
    public interface IWindowManager
    {
        Task<byte[]?> ShowCaptureWindowAsync(Func<byte[], System.Windows.Rect, Task>? onCaptureProcessing = null);
        void ShowTranslationPopup(string text, System.Windows.Rect? placementTarget = null);
        
        void CloseWindow(object viewModel);
        void ShowLauncherWindow();
    }
}
