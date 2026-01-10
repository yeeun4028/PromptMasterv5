using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using PromptMasterv5.ViewModels;
using System.Text;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using PromptMasterv5.Models;

// ★★★ 引用 InputMode，解决引用歧义 ★★★
using InputMode = PromptMasterv5.Models.InputMode;

// 解决控件引用歧义
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using WinFormsCursor = System.Windows.Forms.Cursor;

namespace PromptMasterv5
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        // 分别记录两个输入区域的按键时间，互不干扰
        private DateTime _lastVarEnterTime = DateTime.MinValue;
        private DateTime _lastAddEnterTime = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // ★★★ 核心方法：统一发送处理流程 ★★★
        private async void TriggerSendProcess(TextBox sourceBox, InputMode mode)
        {
            // 1. 强制同步数据 (确保 ViewModel 拿到最新文本)
            sourceBox?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            // 2. 强制隐藏窗口 (立即响应用户)
            this.Hide();

            // 3. 根据传入的模式执行发送 (使用 await 等待)
            if (mode == InputMode.SmartFocus)
            {
                // 场景 A: Ctrl+Enter / 列表双击 -> 智能回退
                await ViewModel.SendBySmartFocus();
            }
            else
            {
                // 场景 B: 双击 Enter -> 强制坐标点击
                await ViewModel.SendByCoordinate();
            }
        }

        // ==========================================
        // BLOCK3: 变量输入框 (逻辑已增强，支持双击Enter)
        // ==========================================
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox == null) return;

                bool isCtrlEnter = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

                // 计算双击
                var now = DateTime.Now;
                var span = (now - _lastVarEnterTime).TotalMilliseconds;
                bool isDoubleEnter = span < 500;

                // 1. Ctrl + Enter -> 智能回退 (Smart Focus)
                if (isCtrlEnter)
                {
                    e.Handled = true;
                    TriggerSendProcess(textBox, InputMode.SmartFocus);
                    return;
                }

                // 2. 双击 Enter -> 强制坐标点击 (Coordinate Click)
                if (isDoubleEnter)
                {
                    e.Handled = true;
                    TriggerSendProcess(textBox, InputMode.CoordinateClick);
                    _lastVarEnterTime = DateTime.MinValue; // 重置
                    return;
                }

                // 3. 如果都不是发送指令，则执行原本的“智能有序列表”逻辑
                _lastVarEnterTime = now; // 记录本次按键时间

                // --- 以下是列表自动编号逻辑 ---
                int caretIndex = textBox.CaretIndex;
                int lineIndex = textBox.GetLineIndexFromCharacterIndex(caretIndex);
                if (lineIndex < 0) return;

                string lineText = textBox.GetLineText(lineIndex);
                var match = Regex.Match(lineText, @"^(\s*)(\d+)\.(\s+)");

                if (match.Success)
                {
                    string indentation = match.Groups[1].Value;
                    int currentNumber = int.Parse(match.Groups[2].Value);
                    string spacing = match.Groups[3].Value;

                    int nextNumber = currentNumber + 1;
                    string insertText = $"\n{indentation}{nextNumber}.{spacing}";

                    textBox.SelectedText = insertText;
                    textBox.CaretIndex += insertText.Length;
                    e.Handled = true;
                }
            }
        }

        // ==========================================
        // BLOCK4: 附加输入框 (逻辑已增强)
        // ==========================================
        private void AdditionalInputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox == null) return;

                bool isCtrlEnter = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

                // 计算双击
                var now = DateTime.Now;
                var span = (now - _lastAddEnterTime).TotalMilliseconds;
                bool isDoubleEnter = span < 500;

                if (isCtrlEnter)
                {
                    // 场景 1：Ctrl + Enter -> 智能回退
                    e.Handled = true;
                    ViewModel.AdditionalInput = textBox.Text;
                    TriggerSendProcess(textBox, InputMode.SmartFocus);
                }
                else if (isDoubleEnter)
                {
                    // 场景 2：双击 Enter -> 强制坐标点击
                    e.Handled = true;
                    ViewModel.AdditionalInput = textBox.Text;
                    TriggerSendProcess(textBox, InputMode.CoordinateClick);
                    _lastAddEnterTime = DateTime.MinValue;
                }
                else
                {
                    // 既不是发送指令 -> 记录时间，允许正常换行
                    _lastAddEnterTime = now;
                }
            }
        }

        // ==========================================
        // 列表双击 -> 智能回退 (Smart Focus)
        // ==========================================
        private async void FileListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                // 使用 await 等待异步任务，消除警告
                await ViewModel.SendBySmartFocus();
            }
        }

        // ... 以下辅助代码保持不变 ...

        private void WebDavPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;
            if (pb != null && ViewModel.Config != null && pb.Password != ViewModel.Config.Password)
                pb.Password = ViewModel.Config.Password;
        }

        private void WebDavPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;
            if (pb != null && ViewModel.Config != null)
                ViewModel.Config.Password = pb.Password;
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin)
                return;

            e.Handled = true;
            var sb = new StringBuilder();
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) sb.Append("Ctrl+");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) sb.Append("Alt+");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) sb.Append("Shift+");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) sb.Append("Win+");
            sb.Append(key.ToString());

            var textBox = sender as TextBox;
            if (textBox != null)
            {
                ViewModel.Config.GlobalHotkey = sb.ToString();
                textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            }
        }

        private async void PickCoordinate_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            string originalContent = btn.Content.ToString() ?? "拾取";
            try
            {
                btn.IsEnabled = false;
                for (int i = 3; i > 0; i--)
                {
                    btn.Content = $"{i}";
                    await Task.Delay(1000);
                }
                var pt = WinFormsCursor.Position;
                ViewModel.LocalConfig.ClickX = pt.X;
                ViewModel.LocalConfig.ClickY = pt.Y;
                btn.Content = "已获取!";
                await Task.Delay(1000);
            }
            finally
            {
                btn.Content = originalContent;
                btn.IsEnabled = true;
            }
        }
    }
}