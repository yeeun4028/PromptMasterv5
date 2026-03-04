using System.Windows;
using System.Windows.Input;
using PromptMasterv5.ViewModels;

namespace PromptMasterv5.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        public void SetDataContext(MainViewModel viewModel)
        {
            DataContext = viewModel;
            SettingsViewContent.DataContext = viewModel;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Window_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
            e.Handled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel mainVM)
            {
                mainVM.SettingsVM.IsSettingsOpen = false;
            }
        }
    }
}
