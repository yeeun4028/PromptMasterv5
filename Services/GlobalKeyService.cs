using Gma.System.MouseKeyHook;
using System;
using System.Windows.Forms;

namespace PromptMasterv5.Services
{
    public class GlobalKeyService : IDisposable
    {
        private IKeyboardMouseEvents? _globalHook;

        // Ctrl 双击相关
        private DateTime _lastCtrlPressTime = DateTime.MinValue;
        private const int DoubleClickInterval = 400;

        // ★★★ 新增：分号双击相关 ★★★
        private char _lastChar = '\0';
        private DateTime _lastCharTime = DateTime.MinValue;

        public event EventHandler? OnDoubleCtrlDetected;
        // 定义双击分号事件
        public event EventHandler? OnDoubleSemiColonDetected;

        public void Start()
        {
            if (_globalHook != null) return;

            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyUp += GlobalHook_KeyUp;
            // 订阅字符输入事件
            _globalHook.KeyPress += GlobalHook_KeyPress;
        }

        private void GlobalHook_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey || e.KeyCode == Keys.ControlKey)
            {
                var now = DateTime.Now;
                var span = (now - _lastCtrlPressTime).TotalMilliseconds;

                if (span > 50 && span < DoubleClickInterval)
                {
                    OnDoubleCtrlDetected?.Invoke(this, EventArgs.Empty);
                    _lastCtrlPressTime = DateTime.MinValue;
                }
                else _lastCtrlPressTime = now;
            }
        }

        // ★★★ 新增：字符监听逻辑 ★★★
        private void GlobalHook_KeyPress(object? sender, KeyPressEventArgs e)
        {
            char currentChar = e.KeyChar;

            // 兼容英文分号 ';' and 中文分号 '；'
            if (currentChar == ';' || currentChar == '；')
            {
                var now = DateTime.Now;
                var span = (now - _lastCharTime).TotalMilliseconds;

                // 判断是否连续按下
                if ((_lastChar == ';' || _lastChar == '；') && span < 800)
                {
                    OnDoubleSemiColonDetected?.Invoke(this, EventArgs.Empty);
                    _lastChar = '\0'; // 重置防止三连击触发两次
                    _lastCharTime = DateTime.MinValue;
                }
                else
                {
                    _lastChar = currentChar;
                    _lastCharTime = now;
                }
            }
            else
            {
                _lastChar = '\0'; // 打断连击
            }
        }

        public void Stop()
        {
            if (_globalHook != null)
            {
                _globalHook.KeyUp -= GlobalHook_KeyUp;
                _globalHook.KeyPress -= GlobalHook_KeyPress;
                _globalHook.Dispose();
                _globalHook = null;
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}