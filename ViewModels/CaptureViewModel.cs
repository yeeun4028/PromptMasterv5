using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptMasterv5;
using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clipboard = System.Windows.Forms.Clipboard;

using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using System.Collections.Generic;

namespace PromptMasterv5.ViewModels
{
    public partial class CaptureViewModel : ObservableObject
    {
        private readonly IWindowManager _windowManager;
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
        
        // Multi-turn chat
        [ObservableProperty]
        private string followUpText = "";
        
        private readonly List<ChatMessage> _conversationHistory = new();
        
        // For history archiving
        private string _lastUserQuestion = "";

        public CaptureViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
            
            // Resolve services manually since we are creating this VM directly in Window
            var services = ((App)System.Windows.Application.Current).ServiceProvider;
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
            try
            {
                // Clear clipboard first to detect new copy
                Clipboard.Clear();
                
                SendKeys.SendWait("^c");
                
                // 2. 等待剪贴板更新
                // Retry loop for clipboard access
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(100);
                    if (Clipboard.ContainsText()) break;
                }
                
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
            catch (Exception)
            {
                // Clipboard access failed (e.g. locked by another process)
                // Just ignore, _capturedText will be empty
            }
        }

        [RelayCommand]
        private async Task SendFollowUp()
        {
            if (string.IsNullOrWhiteSpace(FollowUpText) || _aiService == null) return;
            
            var userMsg = FollowUpText;
            FollowUpText = ""; // Clear input
            IsLoading = true;
            
            _conversationHistory.Add(ChatMessage.FromUser(userMsg));
            
            // Call AI Service with history
            var config = ((App)System.Windows.Application.Current).ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            if (config != null)
            {
                ResultText = ""; // Clear to show new answer
                // Or maybe we want to keep history visible? 
                // The requirement says "AI continues to optimize result".
                // Usually this means replacing the old result with the new one.
                
                var fullResponseBuilder = new System.Text.StringBuilder();
                var sw = Stopwatch.StartNew();
                
                // Use new overload supporting message list
                await foreach (var chunk in _aiService.ChatStreamAsync(_conversationHistory, config.Config))
                {
                    fullResponseBuilder.Append(chunk);

                    if (sw.ElapsedMilliseconds >= 50)
                    {
                        ResultText = fullResponseBuilder.ToString();
                        sw.Restart();
                    }
                }
                
                ResultText = fullResponseBuilder.ToString();
                
                var fullResponse = fullResponseBuilder.ToString();
                _conversationHistory.Add(ChatMessage.FromAssistant(fullResponse));
                
                // Archive update
                if (_dataService != null && !string.IsNullOrEmpty(ResultText))
                {
                    _ = _dataService.ArchiveQuickActionHistoryAsync($"[Follow-up] {userMsg}", ResultText);
                }
            }
            else
            {
                ResultText = "Error: Configuration service not found.";
            }
            
            IsLoading = false;
        }

        [RelayCommand]
        private async Task ExecuteAction(PromptItem action)
        {
            if (_aiService == null) return;
            
            IsLoading = true;
            ResultText = "";
            _conversationHistory.Clear(); // New session
            
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
            
            _lastUserQuestion = finalText;
            _conversationHistory.Add(ChatMessage.FromUser(finalText));
            
            // Call AI Service
            // Streaming support
            var config = ((App)System.Windows.Application.Current).ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            if (config != null)
            {
                ResultText = ""; // Clear before streaming
                
                var fullResponseBuilder = new System.Text.StringBuilder();
                var sw = Stopwatch.StartNew();
                
                await foreach (var chunk in _aiService.ChatStreamAsync(finalText, config.Config))
                {
                    fullResponseBuilder.Append(chunk);

                    if (sw.ElapsedMilliseconds >= 50)
                    {
                        ResultText = fullResponseBuilder.ToString();
                        sw.Restart();
                    }
                }
                
                ResultText = fullResponseBuilder.ToString();
                
                var fullResponse = fullResponseBuilder.ToString();
                _conversationHistory.Add(ChatMessage.FromAssistant(fullResponse));
                
                // Archive history
                if (_dataService != null && !string.IsNullOrEmpty(ResultText))
                {
                    // Fire and forget, don't block UI
                    _ = _dataService.ArchiveQuickActionHistoryAsync(_lastUserQuestion, ResultText);
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
            _windowManager.CloseWindow(this);
        }
    }
}
