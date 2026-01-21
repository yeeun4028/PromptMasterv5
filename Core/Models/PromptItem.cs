using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace PromptMasterv5.Core.Models
{
    public partial class PromptItem : ObservableObject
    {
        // 文件唯一标识
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // 文件标题
        [ObservableProperty]
        private string title = string.Empty;

        // 文件内容 (Markdown)
        [ObservableProperty]
        private string? content;

        // 最后修改时间
        [ObservableProperty]
        private DateTime lastModified;

        // 所属文件夹ID
        public string? FolderId { get; set; }

        // ★★★ 新增：自定义图标 SVG 路径代码 ★★★
        [ObservableProperty]
        private string? iconGeometry;

        // ★★★ 新增：全局划词助手相关字段 ★★★
        [ObservableProperty]
        private bool isQuickAction; // 是否出现在快捷窗口

        [ObservableProperty]
        private string? boundModelId; // 绑定的特定模型ID，为空则跟随全局
    }
}