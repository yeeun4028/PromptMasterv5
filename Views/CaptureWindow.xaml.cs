using PromptMasterv5.Infrastructure.Services;
using PromptMasterv5.ViewModels;
using System.Windows;

namespace PromptMasterv5.Views
{
    public partial class CaptureWindow : Window
    {
        private readonly WindowPositionService _positionService = new();

        // Compatibility for IWindowManager
        public byte[]? CapturedImageBytes { get; private set; }

        public CaptureWindow()
        {
            InitializeComponent();
            Loaded += CaptureWindow_Loaded;
            Deactivated += (s, e) => 
            {
                // Only close if we are not debugging (optional, for convenience)
                // and if the window is actually visible (to avoid re-entry issues)
                if (IsVisible)
                {
                    Close();
                }
            };
            
            // Handle Esc key
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                }
            };
        }

        private void CaptureWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _positionService.PositionWindowNearMouse(this);
            
            // Focus search box or first item
            // this.Focus();
        }
    }
}