using PromptMasterv5.Core.Models;

namespace PromptMasterv5.Core.Interfaces
{
    /// <summary>
    /// 提供应用程序配置管理服务
    /// 所有 ViewModel 通过此服务访问和修改配置，确保配置的一致性
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 获取应用程序全局配置（API Key、模型等）
        /// </summary>
        AppConfig Config { get; }

        /// <summary>
        /// 获取本地用户配置（UI 状态、偏好设置等）
        /// </summary>
        LocalSettings LocalConfig { get; }

        /// <summary>
        /// 保存全局配置到磁盘
        /// </summary>
        void SaveConfig();

        /// <summary>
        /// 保存本地配置到磁盘
        /// </summary>
        void SaveLocalConfig();

        /// <summary>
        /// 重新加载配置（用于从云端恢复后刷新）
        /// </summary>
        void ReloadConfigs();
    }
}
