using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation; // 需要引用 UIAutomationClient 和 UIAutomationTypes (WPF默认包含)

namespace PromptMasterv5.Services
{
    public class BrowserAutomationService : IDisposable
    {
        private NativeMethods.WinEventDelegate? _winEventProc;
        private IntPtr _hookId = IntPtr.Zero;

        // 预设匹配规则
        private readonly string[] _targetUrls = new[]
        {
            "aistudio.google.com",
            "gemini.google.com",
            "deepseek.com",
            "chatgpt.com"
        };

        // 支持的浏览器进程名 (小写)
        private readonly string[] _targetBrowsers = new[] { "chrome", "msedge" };

        // 定义事件：当匹配到目标网站时触发
        public event EventHandler? OnTargetSiteMatched;

        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;

            _winEventProc = new NativeMethods.WinEventDelegate(WinEventProc);
            // 监听前台窗口切换事件
            _hookId = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventProc,
                0,
                0,
                NativeMethods.WINEVENT_OUTOFCONTEXT);
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == NativeMethods.EVENT_SYSTEM_FOREGROUND)
            {
                // 异步检查，防止阻塞 UI 线程或 Hook 回调
                Task.Run(() => CheckActiveWindow(hwnd));
            }
        }

        private void CheckActiveWindow(IntPtr hwnd)
        {
            try
            {
                // 1. 获取进程信息
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                using var process = Process.GetProcessById((int)pid);
                string processName = process.ProcessName.ToLower();

                // 2. 过滤非浏览器进程
                if (!_targetBrowsers.Contains(processName)) return;

                // 3. 使用 UI Automation 读取地址栏
                // 注意：这可能比较耗时，所以我们在 Task 中运行
                string url = GetBrowserUrl(hwnd);

                if (string.IsNullOrEmpty(url)) return;

                // 4. 匹配规则
                if (_targetUrls.Any(target => url.Contains(target, StringComparison.OrdinalIgnoreCase)))
                {
                    // 触发事件
                    OnTargetSiteMatched?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception)
            {
                // 忽略权限不足或句柄无效导致的异常
            }
        }

        private string GetBrowserUrl(IntPtr hwnd)
        {
            try
            {
                // 获取窗口的自动化元素
                var root = AutomationElement.FromHandle(hwnd);
                if (root == null) return string.Empty;

                // Chrome 和 Edge 的地址栏通常是一个 Edit 控件
                // 为了性能，我们只查找 Edit 类型的控件
                var editCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);

                // 查找所有 Edit 控件 (通常地址栏在比较顶层，或者我们遍历前几个)
                var elementCollection = root.FindAll(TreeScope.Descendants, editCondition);

                foreach (AutomationElement element in elementCollection)
                {
                    // 优化：通常地址栏支持 ValuePattern，并且包含 "http" 或域名
                    // 某些浏览器地址栏名称包含 "Address" 或 "地址"
                    if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object patternObj))
                    {
                        var valuePattern = (ValuePattern)patternObj;
                        string value = valuePattern.Current.Value;

                        // 简单验证是否像个网址，或者直接返回让外层匹配
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            // Chrome/Edge 的地址栏通常会暴露当前的 URL
                            // 这里直接返回找到的第一个非空值通常就是地址栏，因为它是焦点的核心
                            // 也可以进一步判断 element.Current.Name
                            return value;
                        }
                    }
                }
            }
            catch
            {
                // UIA 操作可能会因为各种原因失败 (如元素失效)
            }
            return string.Empty;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}