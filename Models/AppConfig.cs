using CommunityToolkit.Mvvm.ComponentModel;

namespace PromptMasterv5.Models
{
    // 继承 ObservableObject 以便界面绑定
    public partial class AppConfig : ObservableObject
    {
        [ObservableProperty]
        private string webDavUrl = "https://dav.jianguoyun.com/dav/";

        [ObservableProperty]
        private string userName = "";

        [ObservableProperty]
        private string password = "";

        // 远程文件夹名称
        public string RemoteFolderName { get; set; } = "PromptMaster";
    }
}