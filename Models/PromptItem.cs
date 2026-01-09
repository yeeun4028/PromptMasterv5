using System;

namespace PromptMasterv3.Models
{
    // 文件模型
    public class PromptItem
    {
        public string Title { get; set; } = "";   // 标题
        public string Content { get; set; } = ""; // 内容
        public DateTime LastModified { get; set; } // 最后修改时间
    }
}