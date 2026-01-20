using System.Windows;
using PromptMasterv5.Infrastructure.Services;

namespace PromptMasterv5.Views
{
    public partial class TranslationPopup : Window
    {
        public TranslationPopup(string initialText)
        {
            InitializeComponent();
            
            // 从配置读取尺寸
            var config = ConfigService.Load();
            this.Width = config.TranslationPopupWidth;
            this.Height = config.TranslationPopupHeight;
            
            ResultBox.Text = initialText;
            
            // 跟随鼠标位置显示，但确保不超出屏幕
            var mouse = System.Windows.Forms.Cursor.Position;
            
            // 获取工作区域（排除任务栏）
            var workArea = SystemParameters.WorkArea;
            
            // 默认显示在鼠标右下方，偏移一点距离
            double left = mouse.X + 15;
            double top = mouse.Y + 15;
            
            // 检查右边界，如果超出则显示在鼠标左侧
            if (left + this.Width > workArea.Right)
            {
                left = mouse.X - this.Width - 15;
            }
            
            // 检查下边界，如果超出则显示在鼠标上方
            if (top + this.Height > workArea.Bottom)
            {
                top = mouse.Y - this.Height - 15;
            }
            
            // 最终确保窗口完全在工作区域内
            left = System.Math.Max(workArea.Left, System.Math.Min(left, workArea.Right - this.Width));
            top = System.Math.Max(workArea.Top, System.Math.Min(top, workArea.Bottom - this.Height));
            
            this.Left = left;
            this.Top = top;
        }

        public void UpdateText(string text)
        {
            ResultBox.Text = text;
        }

        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            // 失去焦点自动关闭
            this.Close();
        }
    }
}