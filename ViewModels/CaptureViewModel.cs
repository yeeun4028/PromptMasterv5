using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptMasterv5;
using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Forms.Clipboard;
using Views = PromptMasterv5.Views;

namespace PromptMasterv5.ViewModels
{
    public partial class CaptureViewModel : ObservableObject
    {
        private readonly IAiService? _aiService;
        private readonly IDataService? _dataService;
        private string _capturedText = "";

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private ObservableCollection<PromptItem> quickActions = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string resultText = "";

        public CaptureViewModel()
        {
            // Resolve services manually since we are creating this VM directly in Window
            var services = ((App)Application.Current).ServiceProvider;
            _aiService = services.GetService(typeof(IAiService)) as IAiService;
            _dataService = services.GetService(typeof(IDataService)) as IDataService;
            
            // 自动执行取词和加载
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadQuickActions();
            await CaptureSelectedText();
        }

        private async Task LoadQuickActions()
        {
            if (_dataService == null) return;
            
            var actions = await _dataService.GetQuickActionsAsync();
            QuickActions.Clear();
            
            // Fallback mock data if empty
            if (actions == null || !actions.Any())
            {
                QuickActions.Add(new PromptItem { Title = "润色", IsQuickAction = true, Content = "请润色以下文本，使其更专业：\n\n{text}" });
                QuickActions.Add(new PromptItem { Title = "解释代码", IsQuickAction = true, Content = "请解释以下代码的逻辑和功能：\n\n{text}" });
                QuickActions.Add(new PromptItem { Title = "中译英", IsQuickAction = true, Content = "请将以下中文翻译成地道的英文：\n\n{text}" });
            }
            else
            {
                foreach (var action in actions)
                {
                    QuickActions.Add(action);
                }
            }
        }

        private async Task CaptureSelectedText()
        {
            // 1. 模拟 Ctrl+C
            SendKeys.SendWait("^c");
            
            // 2. 等待剪贴板更新
            await Task.Delay(100);
            
            // 3. 获取文本
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _capturedText = text;
                }
            }
        }

        [RelayCommand]
        private async Task ExecuteAction(PromptItem action)
        {
            if (_aiService == null) return;
            
            IsLoading = true;
            ResultText = "";
            
            var promptContent = action.Content ?? "";
            var finalText = "";
            
            if (promptContent.Contains("{text}"))
            {
                finalText = promptContent.Replace("{text}", _capturedText);
            }
            else
            {
                finalText = $"{promptContent}\n\n{_capturedText}";
            }
            
            // Call AI Service
            // Streaming support
            var config = ((App)Application.Current).ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            if (config != null)
            {
                ResultText = ""; // Clear before streaming
                await foreach (var chunk in _aiService.ChatStreamAsync(finalText, config.Config))
                {
                    ResultText += chunk;
                }
            }
            else
            {
                ResultText = "Error: Configuration service not found.";
            }
            
            IsLoading = false;
        }

        [RelayCommand]
        private void Copy()
        {
            if (!string.IsNullOrEmpty(ResultText))
            {
                Clipboard.SetText(ResultText);
                CloseWindow();
            }
        }

        [RelayCommand]
        private async Task Replace()
        {
            if (!string.IsNullOrEmpty(ResultText))
            {
                // 1. Copy to clipboard
                Clipboard.SetText(ResultText);
                
                // 2. Close window to return focus to target app
                CloseWindow();
                
                // 3. Wait a bit for focus to settle
                await Task.Delay(200);
                
                // 4. Send Paste (Ctrl+V)
                SendKeys.SendWait("^v");
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            Application.Current.Windows.OfType<Views.CaptureWindow>()
                .FirstOrDefault()?.Close();
        }
    }
}
