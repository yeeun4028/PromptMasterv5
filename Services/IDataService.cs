using PromptMasterv5.Models;
using System.Collections.Generic;
using System.Threading.Tasks; // 必须引用

namespace PromptMasterv5.Services
{
    public interface IDataService
    {
        // 异步加载：返回 Task<AppData>
        Task<AppData> LoadAsync();

        // 异步保存：返回 Task
        Task SaveAsync(IEnumerable<FolderItem> folders, IEnumerable<PromptItem> files);
    }

    public class AppData
    {
        public List<FolderItem> Folders { get; set; } = new();
        public List<PromptItem> Files { get; set; } = new();
    }
}