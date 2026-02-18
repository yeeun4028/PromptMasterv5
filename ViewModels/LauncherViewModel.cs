using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace PromptMasterv5.ViewModels
{
    public partial class LauncherViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<LauncherItem> items = new();

        public LauncherViewModel()
        {
            InitializeItems();
        }

        private void InitializeItems()
        {
            Items.Add(new LauncherItem
            {
                Title = "命令提示符",
                IconGeometry = "M20,19V7H4V19H20M20,3A2,2 0 0,1 22,5V19A2,2 0 0,1 20,21H4A2,2 0 0,1 2,19V5C2,3.89 2.9,3 4,3H20M13,17V15H18V17H13M9.68,13.69L8.27,15.11L5.44,12.28L8.27,9.44L9.68,10.86L8.27,12.28L9.68,13.69Z", // Monitor/Console icon
                Action = () => Process.Start(new ProcessStartInfo("cmd.exe") { UseShellExecute = true })
            });

            Items.Add(new LauncherItem
            {
                Title = "Google",
                IconGeometry = "M21.35,11.1H12.18V13.83H18.69C18.36,17.64 15.19,19.27 12.19,19.27C8.36,19.27 5,16.25 5,12.69C5,9.15 8.2,6.13 12.18,6.13C14.01,6.13 15.33,6.73 16.41,7.74L18.5,5.6C16.89,4.19 14.76,3.27 12.18,3.27C7.12,3.27 3,7.21 3,12.69C3,18.17 7.12,22.11 12.18,22.11C17.06,22.11 20.88,18.96 21.05,14.4H21.35V11.1Z",
                Action = () => Process.Start(new ProcessStartInfo("https://www.google.com") { UseShellExecute = true })
            });
            
             Items.Add(new LauncherItem
            {
                Title = "新建 Excel",
                IconGeometry = "M21.17 3.25Q21.5 3.25 21.76 3.5 22 3.74 22 4.08V19.92Q22 20.26 21.76 20.5 21.5 20.75 21.17 20.75H7.83Q7.5 20.75 7.24 20.5 7 20.26 7 19.92V17H2.83Q2.5 17 2.24 16.76 2 16.5 2 16.17V7.83Q2 7.5 2.24 7.24 2.5 7 2.83 7H7V4.08Q7 3.74 7.24 3.5 7.5 3.25 7.83 3.25M7 13.06L8.18 15.28H9.97L8 12.06L9.93 8.89H8.22L7.13 10.9L7.09 10.96L7.06 11.03Q6.8 10.5 6.5 9.96 6.25 9.43 5.97 8.89H4.27L6.22 12.06L4.3 15.28H6.19L7.09 13.06M8.33 12.5V3.75L21.5 3.75V20.25L8.33 20.25V12.5Z", // Excel icon (approx)
                Action = CreateNewExcel
            });

             Items.Add(new LauncherItem
            {
                Title = "记事本",
                IconGeometry = "M14,10H19.5L14,4.5V10M5,3H15L21,9V19A2,2 0 0,1 19,21H5C3.89,21 3,20.1 3,19V5C3,3.89 3.89,3 5,3M5,12V14H19V12H5M5,16V18H14V16H5Z",
                Action = () => Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true })
            });
        }

        private void CreateNewExcel()
        {
            try
            {
                 // Try to open Excel directly or create a file
                 // Creating a temp file and opening it uses the default handler
                 string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                 string filename = $"New Worksheet {DateTime.Now:yyyyMMddHHmmss}.xlsx";
                 string path = Path.Combine(desktop, filename);
                 
                 // Create empty file? No, Excel needs valid format.
                 // Better to just launch Excel.
                 Process.Start(new ProcessStartInfo("excel.exe") { UseShellExecute = true });
            }
            catch
            {
                 // If excel fails, try open .csv
                 try 
                 {
                     Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true }); // Fallback to calc for test or just alert
                     System.Windows.MessageBox.Show("无法启动 Excel，请确认已安装 Office。", "错误");
                 }
                 catch {}
            }
        }

        [RelayCommand]
        private void ExecuteItem(LauncherItem item)
        {
            try
            {
                item?.Action?.Invoke();
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行失败: {ex.Message}");
            }
        }

        public Action RequestClose { get; set; }
    }

    public class LauncherItem
    {
        public string Title { get; set; }
        public string IconGeometry { get; set; }
        public Action Action { get; set; }
    }
}
