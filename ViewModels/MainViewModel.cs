using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using PromptMasterv3.Models;
using PromptMasterv3.Services;

namespace PromptMasterv3.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly FileDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<FolderItem> folders;

        [ObservableProperty]
        private ObservableCollection<PromptItem> files;

        // ★★★ 新增：当前选中的文件 ★★★
        [ObservableProperty]
        private PromptItem selectedFile;

        // ★★★ 新增：是否处于编辑模式 (默认 false = 预览) ★★★
        [ObservableProperty]
        private bool isEditMode;

        public MainViewModel()
        {
            _dataService = new FileDataService();
            var data = _dataService.Load();

            if (data.Folders.Count == 0)
            {
                data.Folders.Add(new FolderItem { Name = "我的提示词", IsSelected = true });
                data.Folders.Add(new FolderItem { Name = "工作项目" });
            }

            Folders = new ObservableCollection<FolderItem>(data.Folders);
            Files = new ObservableCollection<PromptItem>(data.Files);
        }

        // 当切换文件时，自动退出编辑模式，并保存数据
        partial void OnSelectedFileChanged(PromptItem value)
        {
            IsEditMode = false;
            SaveData();
        }

        // 切换编辑/预览状态的命令
        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            // 如果切换回预览模式（相当于编辑完成），顺便保存一下
            if (!IsEditMode)
            {
                SaveData();
            }
        }

        [RelayCommand]
        private void CreateFolder()
        {
            Folders.Add(new FolderItem { Name = $"新建文件夹 {Folders.Count + 1}" });
            SaveData();
        }

        [RelayCommand]
        private void CreateFile()
        {
            var newFile = new PromptItem
            {
                Title = $"新建文档 {Files.Count + 1}",
                Content = "# 新文档\n请输入内容...",
                LastModified = DateTime.Now
            };
            Files.Add(newFile);
            SelectedFile = newFile; // 自动选中新文件
            IsEditMode = true;      // 自动进入编辑模式
            SaveData();
        }

        private void SaveData()
        {
            _dataService.Save(Folders, Files);
        }
    }
}