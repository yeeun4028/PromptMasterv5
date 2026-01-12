using CommunityToolkit.Mvvm.ComponentModel;

namespace PromptMasterv5.Models
{
    public partial class AppConfig : ObservableObject
    {
        [ObservableProperty]
        private string webDavUrl = "https://dav.jianguoyun.com/dav/";

        [ObservableProperty]
        private string userName = "";

        [ObservableProperty]
        private string password = "";

        [ObservableProperty]
        private string globalHotkey = "Alt+Space";

        [ObservableProperty]
        private bool enableDoubleCtrl = true;

        // ★★★ 新增：AI 配置项 ★★★

        [ObservableProperty]
        private string aiBaseUrl = "https://api.deepseek.com"; // 默认 DeepSeek

        [ObservableProperty]
        private string aiApiKey = "";

        [ObservableProperty]
        private string aiModel = "deepseek-chat"; // 默认模型名

        public string RemoteFolderName { get; set; } = "PromptMaster";
    }
}