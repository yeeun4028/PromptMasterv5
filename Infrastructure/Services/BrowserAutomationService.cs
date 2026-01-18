using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace PromptMasterv5.Infrastructure.Services
{
    public class BrowserAutomationService : IDisposable
    {
        private NativeMethods.WinEventDelegate? _winEventProc;
        private IntPtr _hookId = IntPtr.Zero;

        private readonly string[] _targetUrls = new[]
        {
            "aistudio.google.com",
            "gemini.google.com",
            "deepseek.com",
            "chatgpt.com"
        };

        private readonly string[] _targetBrowsers = new[] { "chrome", "msedge" };

        public event EventHandler? OnTargetSiteMatched;

        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;

            _winEventProc = new NativeMethods.WinEventDelegate(WinEventProc);
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
                Task.Run(() => CheckActiveWindow(hwnd));
            }
        }

        private void CheckActiveWindow(IntPtr hwnd)
        {
            try
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                using var process = Process.GetProcessById((int)pid);
                string processName = process.ProcessName.ToLower();

                if (!_targetBrowsers.Contains(processName)) return;

                string url = GetBrowserUrl(hwnd);

                if (string.IsNullOrEmpty(url)) return;

                if (_targetUrls.Any(target => url.Contains(target, StringComparison.OrdinalIgnoreCase)))
                {
                    OnTargetSiteMatched?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception)
            {
            }
        }

        private string GetBrowserUrl(IntPtr hwnd)
        {
            try
            {
                var root = AutomationElement.FromHandle(hwnd);
                if (root == null) return string.Empty;

                var editCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);

                var elementCollection = root.FindAll(TreeScope.Descendants, editCondition);

                foreach (AutomationElement element in elementCollection)
                {
                    if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object patternObj))
                    {
                        var valuePattern = (ValuePattern)patternObj;
                        string value = valuePattern.Current.Value;

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }
            catch
            {
            }
            return string.Empty;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
