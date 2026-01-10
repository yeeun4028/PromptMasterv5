using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Threading.Tasks;
using GongSolutions.Wpf.DragDrop;
using PromptMasterv5.Models;
using PromptMasterv5.Services;

namespace PromptMasterv5.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private bool _isCreatingFile = false;

        // ★★★ 新增：配置对象，绑定到设置界面 ★★★
        [ObservableProperty]
        private AppConfig config;

        // ★★★ 新增：控制设置弹窗是否显示 ★★★
        [ObservableProperty]
        private bool isSettingsOpen = false;

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        [ObservableProperty]
        private ICollectionView? filesView;

        // 强制使用 WebDAV 服务，通过 Config 控制是否启用
        private bool _useLocalMode = false;

        public IDropTarget FolderDropHandler { get; private set; }

        [ObservableProperty]
        private bool isNavigationVisible = true;

        [ObservableProperty]
        private ObservableCollection<FolderItem> folders = new();

        [ObservableProperty]
        private FolderItem? selectedFolder;

        [ObservableProperty]
        private ObservableCollection<PromptItem> files = new();

        [ObservableProperty]
        private PromptItem? selectedFile;

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private ObservableCollection<VariableItem> variables = new();

        [ObservableProperty]
        private bool hasVariables;

        [ObservableProperty]
        private string additionalInput = "";

        public MainViewModel()
        {
            // 加载配置
            Config = ConfigService.Load();

            if (_useLocalMode)
                _dataService = new FileDataService();
            else
                _dataService = new WebDavDataService();

            FolderDropHandler = new FolderDropHandler(this);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            StatusMessage = "正在加载数据...";
            var data = await _dataService.LoadAsync();

            if (data.Folders.Count == 0)
            {
                data.Folders.Add(new FolderItem { Name = "我的提示词" });
            }
            Folders = new ObservableCollection<FolderItem>(data.Folders);
            Files = new ObservableCollection<PromptItem>(data.Files);

            var defaultFolderId = Folders.First().Id;
            foreach (var file in Files)
            {
                if (string.IsNullOrEmpty(file.FolderId)) file.FolderId = defaultFolderId;
            }

            var view = CollectionViewSource.GetDefaultView(Files);
            if (view != null)
            {
                view.Filter = FilterFiles;
            }
            FilesView = view;

            Files.CollectionChanged += (s, e) => RequestSave();

            SelectedFolder = Folders.First();
            StatusMessage = "加载完成";

            await Task.Delay(2000);
            StatusMessage = "";
        }

        private async void RequestSave()
        {
            // 如果没填密码，就不自动保存，避免报错烦人
            if (string.IsNullOrEmpty(Config.UserName) || string.IsNullOrEmpty(Config.Password)) return;
            await SaveDataAsync();
        }

        private async Task SaveDataAsync()
        {
            StatusMessage = "正在同步...";
            try
            {
                await _dataService.SaveAsync(Folders, Files);
                StatusMessage = "同步完成";
            }
            catch (Exception ex)
            {
                StatusMessage = "同步失败: " + ex.Message;
            }
        }

        // ★★★ 打开设置命令 ★★★
        [RelayCommand]
        private void OpenSettings()
        {
            // 重新从磁盘加载，防止意外覆盖
            Config = ConfigService.Load();
            IsSettingsOpen = true;
        }

        // ★★★ 保存设置并关闭命令 ★★★
        [RelayCommand]
        private void SaveSettings()
        {
            ConfigService.Save(Config);
            IsSettingsOpen = false;
            StatusMessage = "设置已保存";
        }

        // ★★★ 手动备份命令 (推送到云端) ★★★
        [RelayCommand]
        private async Task ManualBackup()
        {
            ConfigService.Save(Config); // 先保存配置
            StatusMessage = "正在上传备份...";
            try
            {
                await _dataService.SaveAsync(Folders, Files);
                MessageBox.Show("成功备份到远程服务器！", "备份成功");
                StatusMessage = "备份成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"备份失败：{ex.Message}", "错误");
                StatusMessage = "备份失败";
            }
        }

        // ★★★ 手动恢复命令 (从云端拉取) ★★★
        [RelayCommand]
        private async Task ManualRestore()
        {
            ConfigService.Save(Config); // 先保存配置
            if (MessageBox.Show("确定要从远程恢复吗？\n这将覆盖本地当前所有未保存的修改！", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            StatusMessage = "正在从远程下载...";
            try
            {
                var data = await _dataService.LoadAsync();
                if (data.Files.Count == 0 && data.Folders.Count == 0)
                {
                    MessageBox.Show("远程似乎没有数据，或者下载失败。", "提示");
                    return;
                }

                // 重新加载数据到界面
                Folders = new ObservableCollection<FolderItem>(data.Folders);
                Files = new ObservableCollection<PromptItem>(data.Files);
                // 重新绑定视图
                var view = CollectionViewSource.GetDefaultView(Files);
                if (view != null) view.Filter = FilterFiles;
                FilesView = view;
                // 重新挂载事件
                Files.CollectionChanged += (s, e) => RequestSave();
                SelectedFolder = Folders.FirstOrDefault();

                MessageBox.Show("成功从远程恢复数据！", "恢复成功");
                StatusMessage = "恢复成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复失败：{ex.Message}", "错误");
                StatusMessage = "恢复失败";
            }
        }

        [RelayCommand]
        private void ToggleNavigation() => IsNavigationVisible = !IsNavigationVisible;

        public void MoveFileToFolder(PromptItem file, FolderItem targetFolder)
        {
            if (file == null || targetFolder == null || file.FolderId == targetFolder.Id) return;
            file.FolderId = targetFolder.Id;
            FilesView?.Refresh();
            if (SelectedFile == file) SelectedFile = null;
            RequestSave();
        }

        partial void OnSelectedFolderChanged(FolderItem? value)
        {
            SelectedFile = null;
            FilesView?.Refresh();
        }

        private bool FilterFiles(object obj)
        {
            if (obj is PromptItem file && SelectedFolder != null)
                return file.FolderId == SelectedFolder.Id;
            return false;
        }

        partial void OnSelectedFileChanged(PromptItem? oldValue, PromptItem? newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= SelectedFile_PropertyChanged;
            if (newValue != null)
            {
                newValue.PropertyChanged += SelectedFile_PropertyChanged;
                ParseVariables();
            }
            else
            {
                Variables.Clear();
                HasVariables = false;
            }

            if (_isCreatingFile) return;
            IsEditMode = false;
        }

        private void SelectedFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PromptItem.Content)) ParseVariables();
            RequestSave();
        }

        private void ParseVariables()
        {
            if (SelectedFile == null)
            {
                Variables.Clear();
                HasVariables = false;
                return;
            }
            var content = SelectedFile.Content ?? "";
            var matches = Regex.Matches(content, @"\{\{(.*?)\}\}");
            var newVarNames = matches.Cast<Match>()
                                     .Select(m => m.Groups[1].Value.Trim())
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .Distinct()
                                     .ToList();

            for (int i = Variables.Count - 1; i >= 0; i--)
            {
                if (!newVarNames.Contains(Variables[i].Name)) Variables.RemoveAt(i);
            }
            foreach (var name in newVarNames)
            {
                if (!Variables.Any(v => v.Name == name)) Variables.Add(new VariableItem { Name = name });
            }
            HasVariables = Variables.Count > 0;
        }

        [RelayCommand]
        private void CopyCompiledText()
        {
            if (SelectedFile == null) return;
            string finalContent = SelectedFile.Content ?? "";
            if (HasVariables)
            {
                foreach (var variable in Variables)
                {
                    finalContent = finalContent.Replace("{{" + variable.Name + "}}", variable.Value ?? "");
                }
            }
            else if (!string.IsNullOrWhiteSpace(AdditionalInput))
            {
                finalContent += "\n" + AdditionalInput;
            }
            if (!string.IsNullOrEmpty(finalContent)) Clipboard.SetText(finalContent);
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            if (!IsEditMode) RequestSave();
        }

        [RelayCommand]
        private void CreateFolder()
        {
            var newFolder = new FolderItem { Name = $"新建文件夹 {Folders.Count + 1}" };
            Folders.Add(newFolder);
            SelectedFolder = newFolder;
            RequestSave();
        }

        [RelayCommand]
        private void CreateFile()
        {
            if (SelectedFolder == null) return;
            _isCreatingFile = true;
            var newFile = new PromptItem
            {
                Title = "新文档",
                Content = "# 新文档\n你好，我是{{name}}...",
                LastModified = DateTime.Now,
                FolderId = SelectedFolder.Id
            };
            Files.Add(newFile);
            SelectedFile = newFile;
            IsEditMode = true;
            RequestSave();
            _isCreatingFile = false;
        }

        [RelayCommand]
        private void DeleteFile(PromptItem? item)
        {
            var target = item ?? SelectedFile;
            if (target != null)
            {
                Files.Remove(target);
                if (SelectedFile == target) SelectedFile = null;
                RequestSave();
            }
        }
    }
}