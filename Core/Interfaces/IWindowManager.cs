using System;
using System.Threading.Tasks;
using System.Windows;

namespace PromptMasterv5.Core.Interfaces
{
    public interface IWindowManager
    {
        Task<byte[]?> ShowCaptureWindowAsync(Func<byte[], System.Windows.Rect, Task>? onCaptureProcessing = null);
        void ShowTranslationPopup(string text, System.Windows.Rect? placementTarget = null);
    }
}
