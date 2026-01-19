using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PromptMasterv5.Core.Models;
using System.Windows.Automation;

using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace PromptMasterv5.Infrastructure.Services
{
    public class InputSender
    {
        private static (int x, int y) ResolveClickPoint(LocalSettings settings, IntPtr previousWindowHandle)
        {
            try
            {
                string url = TryGetBrowserUrl(previousWindowHandle);

                if (settings.CoordinateRules != null && settings.CoordinateRules.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        foreach (var rule in settings.CoordinateRules)
                        {
                            if (string.IsNullOrWhiteSpace(rule.UrlContains)) continue;
                            if (url.Contains(rule.UrlContains.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                return (rule.X, rule.Y);
                            }
                        }
                    }

                    foreach (var rule in settings.CoordinateRules)
                    {
                        if (string.IsNullOrWhiteSpace(rule.UrlContains))
                        {
                            return (rule.X, rule.Y);
                        }
                    }

                    var first = settings.CoordinateRules[0];
                    return (first.X, first.Y);
                }
            }
            catch { }

            return (settings.ClickX, settings.ClickY);
        }

        private static string TryGetBrowserUrl(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return "";
            try
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                using var process = Process.GetProcessById((int)pid);
                var processName = process.ProcessName.ToLowerInvariant();
                if (processName != "chrome" && processName != "msedge") return "";

                var root = AutomationElement.FromHandle(hwnd);
                if (root == null) return "";

                var editCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
                var edits = root.FindAll(TreeScope.Descendants, editCondition);
                if (edits == null || edits.Count == 0) return "";

                string bestValue = "";
                double bestScore = double.NegativeInfinity;

                foreach (AutomationElement element in edits)
                {
                    if (!element.TryGetCurrentPattern(ValuePattern.Pattern, out object patternObj)) continue;
                    var value = ((ValuePattern)patternObj).Current.Value;
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    if (value.Any(char.IsWhiteSpace)) continue;
                    if (!LooksLikeUrlOrDomain(value)) continue;

                    var rect = element.Current.BoundingRectangle;
                    if (rect.IsEmpty) continue;
                    if (rect.Height < 10 || rect.Height > 80) continue;
                    if (rect.Width < 350) continue;
                    if (rect.Top > 260) continue;

                    var name = element.Current.Name ?? "";
                    var automationId = element.Current.AutomationId ?? "";
                    var hintBoost =
                        (name.Contains("address", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("search", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("地址", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("网址", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("搜索", StringComparison.OrdinalIgnoreCase) ||
                         automationId.Contains("address", StringComparison.OrdinalIgnoreCase))
                            ? 2000
                            : 0;

                    var score = hintBoost + rect.Width - rect.Top * 5;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestValue = value;
                    }
                }

                return bestValue;
            }
            catch { }
            return "";
        }

        private static bool LooksLikeUrlOrDomain(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (value.Length < 4) return false;
            if (value.Contains("://", StringComparison.OrdinalIgnoreCase)) return true;
            if (value.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return true;
            if (value.StartsWith("chrome://", StringComparison.OrdinalIgnoreCase)) return true;
            if (value.StartsWith("edge://", StringComparison.OrdinalIgnoreCase)) return true;
            if (value.Contains('.')) return true;
            return false;
        }

        public static async Task SendAsync(string text, InputMode targetMode, LocalSettings settings, IntPtr previousWindowHandle)
        {
            if (string.IsNullOrEmpty(text)) return;

            bool clipboardSuccess = false;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    clipboardSuccess = true;
                    break;
                }
                catch
                {
                    await Task.Delay(50);
                }
            }

            if (!clipboardSuccess)
            {
                MessageBox.Show("写入剪贴板失败，无法发送。", "错误");
                return;
            }

            await Task.Delay(200);

            if (targetMode == InputMode.SmartFocus)
            {
                if (previousWindowHandle != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(previousWindowHandle);
                    await Task.Delay(150);
                }
            }
            else
            {
                var (x, y) = ResolveClickPoint(settings, previousWindowHandle);
                NativeMethods.SetCursorPos(x, y);
                await Task.Delay(50);

                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                await Task.Delay(50);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                await Task.Delay(200);
            }

            NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, 0);
            await Task.Delay(20);
            NativeMethods.keybd_event(NativeMethods.VK_V, 0, 0, 0);
            await Task.Delay(20);
            NativeMethods.keybd_event(NativeMethods.VK_V, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
            await Task.Delay(20);
            NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, 0);

            await Task.Delay(120);
            for (int i = 0; i < 30; i++)
            {
                if (!NativeMethods.IsKeyDown(NativeMethods.VK_CONTROL) &&
                    !NativeMethods.IsKeyDown(NativeMethods.VK_SHIFT) &&
                    !NativeMethods.IsKeyDown(NativeMethods.VK_MENU))
                {
                    break;
                }
                await Task.Delay(20);
            }

            NativeMethods.keybd_event(NativeMethods.VK_RETURN, 0, 0, 0);
            await Task.Delay(20);
            NativeMethods.keybd_event(NativeMethods.VK_RETURN, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
        }
    }
}
