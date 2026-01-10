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
using GongSolutions.Wpf.DragDrop; // 必须引用：用于拖拽功能
using PromptMasterv5.Models;
using PromptMasterv5.Services;

namespace PromptMasterv5.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private bool _isCreatingFile = false;

        // 文件列表视图（用于过滤）
        public ICollectionView FilesView { get; private set; }

        // ★ 拖拽处理器：暴露给 View 绑定
        public IDropTarget FolderDropHandler { get; private set; }

        // ★ 侧边栏显示状态：默认显示
        [ObservableProperty]
        private bool isNavigationVisible = true;

        [ObservableProperty]
        private ObservableCollection<FolderItem> folders;

        [ObservableProperty]
        private FolderItem? selectedFolder;

        [ObservableProperty]
        private ObservableCollection<PromptItem> files;

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
            _dataService = new FileDataService();
            var data = _dataService.Load();

            // ★ 初始化拖拽处理器 (将 ViewModel 自身传进去，以便处理器调用 MoveFileToFolder)
            FolderDropHandler = new FolderDropHandler(this);

            // 初始化文件夹
            if (data.Folders.Count == 0)
            {
                data.Folders.Add(new FolderItem { Name = "我的提示词" });
            }
            Folders = new ObservableCollection<FolderItem>(data.Folders);

            // 初始化文件
            Files = new ObservableCollection<PromptItem>(data.Files);

            // 数据清洗：如果有老数据没有 FolderId，默认分配给第一个文件夹
            var defaultFolderId = Folders.First().Id;
            foreach (var file in Files)
            {
                if (string.IsNullOrEmpty(file.FolderId))
                {
                    file.FolderId = defaultFolderId;
                }
            }

            // 初始化视图过滤机制
            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = FilterFiles; // 指定过滤函数

            // 监听文件列表变化（用于自动保存）
            Files.CollectionChanged += Files_CollectionChanged;

            // 默认选中第一个文件夹
            SelectedFolder = Folders.First();
        }

        // ★ 新增命令：切换侧边栏显示/隐藏
        [RelayCommand]
        private void ToggleNavigation()
        {
            IsNavigationVisible = !IsNavigationVisible;
        }

        // ★ 新增逻辑：将文件移动到指定文件夹 (供 FolderDropHandler 调用)
        public void MoveFileToFolder(PromptItem file, FolderItem targetFolder)
        {
            if (file == null || targetFolder == null) return;
            if (file.FolderId == targetFolder.Id) return; // 如果已经在该文件夹，不做操作

            // 1. 修改文件的归属 ID
            file.FolderId = targetFolder.Id;

            // 2. 刷新视图 (因为 Filter 是基于 FolderId 的，改了 ID 后它应该从当前列表中消失)
            FilesView.Refresh();

            // 3. 如果当前选中的就是被移走的文件，清空选中状态，避免界面显示错乱
            if (SelectedFile == file)
            {
                SelectedFile = null;
            }

            // 4. 保存更改到硬盘
            SaveData();
        }

        // 当选中的文件夹改变时
        partial void OnSelectedFolderChanged(FolderItem? value)
        {
            SelectedFile = null; // 切换文件夹时清空选中的文件
            FilesView.Refresh(); // 触发过滤，只显示当前文件夹的文件
        }

        // 核心过滤逻辑：只显示 FolderId 匹配的文件
        private bool FilterFiles(object obj)
        {
            if (obj is PromptItem file && SelectedFolder != null)
            {
                return file.FolderId == SelectedFolder.Id;
            }
            return false;
        }

        private void Files_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SaveData();
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
            SaveData();
        }

        private void SelectedFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 如果内容变了，重新解析变量
            if (e.PropertyName == nameof(PromptItem.Content))
            {
                ParseVariables();
            }
            SaveData();
        }

        // 解析变量逻辑：{{variable}}
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

            // 移除不再存在的变量
            for (int i = Variables.Count - 1; i >= 0; i--)
            {
                if (!newVarNames.Contains(Variables[i].Name))
                {
                    Variables.RemoveAt(i);
                }
            }

            // 添加新发现的变量
            foreach (var name in newVarNames)
            {
                if (!Variables.Any(v => v.Name == name))
                {
                    Variables.Add(new VariableItem { Name = name });
                }
            }

            HasVariables = Variables.Count > 0;
        }

        [RelayCommand]
        private void CopyCompiledText()
        {
            if (SelectedFile == null) return;

            string finalContent = SelectedFile.Content ?? "";

            // 替换变量
            if (HasVariables)
            {
                foreach (var variable in Variables)
                {
                    string placeholder = "{{" + variable.Name + "}}";
                    string value = variable.Value ?? "";
                    finalContent = finalContent.Replace(placeholder, value);
                }
            }
            else
            {
                // 如果没有变量，追加附加输入
                if (!string.IsNullOrWhiteSpace(AdditionalInput))
                {
                    finalContent += "\n" + AdditionalInput;
                }
            }

            if (!string.IsNullOrEmpty(finalContent))
            {
                Clipboard.SetText(finalContent);
            }
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            if (!IsEditMode) SaveData();
        }

        [RelayCommand]
        private void CreateFolder()
        {
            var newFolder = new FolderItem { Name = $"新建文件夹 {Folders.Count + 1}" };
            Folders.Add(newFolder);
            SelectedFolder = newFolder; // 选中新文件夹
            SaveData();
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
                FolderId = SelectedFolder.Id // 标记归属
            };

            Files.Add(newFile);
            SelectedFile = newFile;
            IsEditMode = true;

            SaveData();
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
                SaveData();
            }
        }

        private void SaveData()
        {
            _dataService.Save(Folders, Files);
        }
    }
}