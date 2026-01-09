using System.Windows;
using System.Windows.Input;
using PromptMasterv3.ViewModels; // 引用刚才写的 ViewModel

namespace PromptMasterv3
{
    public partial class MainWindow : Window
    {
        // 声明 ViewModel
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            // ★★★ 核心步骤：初始化 ViewModel 并赋值给 DataContext ★★★
            // 这样 XAML 界面里就可以直接用到 ViewModel 里的数据了
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
        }

        // 顶部拖拽逻辑 (保持不变)
        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}