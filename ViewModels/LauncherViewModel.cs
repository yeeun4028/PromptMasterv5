using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Core.Models;
using PromptMasterv5.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PromptMasterv5.ViewModels
{
    public partial class LauncherViewModel : ObservableObject
    {
        private readonly ILauncherService _launcherService;
        private readonly ISettingsService _settingsService;
        private readonly IWindowManager _windowManager;
        private Dictionary<string, int> _itemOrders = new();

        [ObservableProperty]
        private ObservableCollection<LauncherItem> items = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<LauncherItem> filteredItems = new();

        [ObservableProperty]
        private string currentCategory = "Bookmark";

        public LauncherViewModel(
            ILauncherService launcherService, 
            ISettingsService settingsService,
            IWindowManager windowManager)
        {
            _launcherService = launcherService;
            _settingsService = settingsService;
            _windowManager = windowManager;
            
            LoadItemOrders();
            InitializeItems();
        }

        private void LoadItemOrders()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptMasterv5", "launcher_orders.json");
                if (File.Exists(appDataPath))
                {
                    var json = File.ReadAllText(appDataPath);
                    _itemOrders = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new();
                }
            }
            catch
            {
                _itemOrders = new();
            }
        }

        public void MoveItem(LauncherItem source, LauncherItem target)
        {
            if (source == null || target == null || source == target) return;

            var oldIndex = FilteredItems.IndexOf(source);
            var newIndex = FilteredItems.IndexOf(target);

            if (oldIndex < 0 || newIndex < 0) return;

            FilteredItems.Move(oldIndex, newIndex);

            // Update all DisplayOrders based on new index and save
            for (int i = 0; i < FilteredItems.Count; i++)
            {
                var item = FilteredItems[i];
                item.DisplayOrder = i;
                
                var key = $"{item.Category}_{item.Title}";
                _itemOrders[key] = i;
            }
            
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptMasterv5", "launcher_orders.json");
                var dir = Path.GetDirectoryName(appDataPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                var json = JsonSerializer.Serialize(_itemOrders);
                File.WriteAllText(appDataPath, json);
            }
            catch { }
        }

        public void SelectCategory(string category)
        {
            CurrentCategory = category;
            UpdateFilter();
        }

        private async void InitializeItems()
        {
            var staticItems = GetStaticItems();
            foreach (var item in staticItems)
            {
                Items.Add(item);
            }

            await LoadDiscoveredItemsAsync();
            
            UpdateFilter();
        }

        private List<LauncherItem> GetStaticItems()
        {
            return new List<LauncherItem>
            {
                new LauncherItem
                {
                    Title = "Google",
                    IconGeometry = "M21.35,11.1H12.18V13.83H18.69C18.36,17.64 15.19,19.27 12.19,19.27C8.36,19.27 5,16.25 5,12.69C5,9.15 8.2,6.13 12.18,6.13C14.01,6.13 15.33,6.73 16.41,7.74L18.5,5.6C16.89,4.19 14.76,3.27 12.18,3.27C7.12,3.27 3,7.21 3,12.69C3,18.17 7.12,22.11 12.18,22.11C17.06,22.11 20.88,18.96 21.05,14.4H21.35V11.1Z",
                    Category = LauncherCategory.Bookmark,
                    Action = () => Process.Start(new ProcessStartInfo("https://www.google.com") { UseShellExecute = true })
                },
                new LauncherItem
                {
                    Title = "命令提示符",
                    IconGeometry = "M20,19V7H4V19H20M20,3A2,2 0 0,1 22,5V19A2,2 0 0,1 20,21H4A2,2 0 0,1 2,19V5C2,3.89 2.9,3 4,3H20M13,17V15H18V17H13M9.68,13.69L8.27,15.11L5.44,12.28L8.27,9.44L9.68,10.86L8.27,12.28L9.68,13.69Z",
                    Category = LauncherCategory.Application,
                    Action = () => Process.Start(new ProcessStartInfo("cmd.exe") { UseShellExecute = true })
                },
                new LauncherItem
                {
                    Title = "记事本",
                    IconGeometry = "M14,10H19.5L14,4.5V10M5,3H15L21,9V19A2,2 0 0,1 19,21H5C3.89,21 3,20.1 3,19V5C3,3.89 3.89,3 5,3M5,12V14H19V12H5M5,16V18H14V16H5Z",
                    Category = LauncherCategory.Tool,
                    Action = () => Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true })
                }
            };
        }

        private async Task LoadDiscoveredItemsAsync()
        {
            try
            {
                var paths = _settingsService.Config.LauncherSearchPaths;
                if (paths == null || !paths.Any()) return;

                var discovered = await _launcherService.GetItemsAsync(paths);
                foreach (var item in discovered)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load discovered items: {ex.Message}");
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilter();
        }

        private void UpdateFilter()
        {
            var enumCategory = CurrentCategory switch
            {
                "Bookmark" => LauncherCategory.Bookmark,
                "Application" => LauncherCategory.Application,
                "Tool" => LauncherCategory.Tool,
                _ => LauncherCategory.Bookmark
            };

            var filtered = Items.Where(i => i.Category == enumCategory).ToList();
            
            foreach (var item in filtered)
            {
                var key = $"{item.Category}_{item.Title}";
                if (_itemOrders.TryGetValue(key, out var order))
                {
                    item.DisplayOrder = order;
                }
                else
                {
                    item.DisplayOrder = int.MaxValue;
                }
            }

            var ordered = filtered.OrderBy(i => i.DisplayOrder).ToList();
            FilteredItems = new ObservableCollection<LauncherItem>(ordered);
        }

        [RelayCommand]
        private void ExecuteItem(LauncherItem item)
        {
            try
            {
                if (item?.Action != null)
                {
                    item.Action.Invoke();
                }
                else if (!string.IsNullOrEmpty(item?.FilePath))
                {
                    var info = new ProcessStartInfo(item.FilePath) { UseShellExecute = true };
                    
                    if (_settingsService.Config.LauncherRunAsAdmin)
                    {
                        info.Verb = "runas";
                    }

                    Process.Start(info);
                }
                
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Failed to execute launcher item", "LauncherViewModel.ExecuteItem");
            }
        }

        public Action? RequestClose { get; set; }
    }
}
