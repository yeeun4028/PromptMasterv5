namespace PromptMasterv3.Models
{
    // 文件夹模型
    public class FolderItem
    {
        public string Name { get; set; } = ""; // 文件夹名字
        public bool IsSelected { get; set; }   // 是否被选中

        // 这是一个图标路径的占位符，暂时没用，为以后留接口
        public string IconData { get; set; } = ""; // 默认为空字符串
    }
}