using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using PromptMasterv3.Models;

namespace PromptMasterv3.Services
{
    // 定义一个数据包结构，方便一次性保存所有数据
    public class AppData
    {
        public List<FolderItem> Folders { get; set; } = new();
        public List<PromptItem> Files { get; set; } = new();
    }

    public class FileDataService
    {
        // 数据保存路径：就在程序运行的根目录下，名为 data.json
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.json");

        // 保存数据
        public void Save(IEnumerable<FolderItem> folders, IEnumerable<PromptItem> files)
        {
            var data = new AppData
            {
                Folders = new List<FolderItem>(folders),
                Files = new List<PromptItem>(files)
            };

            var options = new JsonSerializerOptions { WriteIndented = true }; // 让 JSON 格式化美观
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, jsonString);
        }

        // 读取数据
        public AppData Load()
        {
            if (!File.Exists(_filePath))
            {
                // 如果文件不存在（第一次运行），返回空数据
                return new AppData();
            }

            try
            {
                string jsonString = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppData>(jsonString) ?? new AppData();
            }
            catch (Exception)
            {
                // 如果文件坏了，就返回空的，防止程序崩溃
                return new AppData();
            }
        }
    }
}