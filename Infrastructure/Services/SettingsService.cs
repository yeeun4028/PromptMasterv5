using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Core.Models;
using PromptMasterv5.Infrastructure.Services;

namespace PromptMasterv5.Infrastructure.Services
{
    /// <summary>
    /// 配置管理服务实现
    /// 作为配置的单一真实来源，所有 ViewModel 通过此服务访问配置
    /// </summary>
    public class SettingsService : ISettingsService
    {
        public AppConfig Config { get; private set; }
        public LocalSettings LocalConfig { get; private set; }

        public SettingsService()
        {
            // 启动时加载配置
            Config = ConfigService.Load();
            LocalConfig = LocalConfigService.Load();

            LoggerService.Instance.LogInfo("Settings loaded successfully", "SettingsService.ctor");
        }

        public void SaveConfig()
        {
            try
            {
                ConfigService.Save(Config);
                LoggerService.Instance.LogInfo("AppConfig saved", "SettingsService.SaveConfig");
            }
            catch (System.Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Failed to save AppConfig", "SettingsService.SaveConfig");
                throw;
            }
        }

        public void SaveLocalConfig()
        {
            try
            {
                LocalConfigService.Save(LocalConfig);
                LoggerService.Instance.LogInfo("LocalSettings saved", "SettingsService.SaveLocalConfig");
            }
            catch (System.Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Failed to save LocalSettings", "SettingsService.SaveLocalConfig");
                throw;
            }
        }

        public void ReloadConfigs()
        {
            try
            {
                Config = ConfigService.Load();
                LocalConfig = LocalConfigService.Load();
                LoggerService.Instance.LogInfo("Settings reloaded", "SettingsService.ReloadConfigs");
            }
            catch (System.Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Failed to reload settings", "SettingsService.ReloadConfigs");
                throw;
            }
        }
    }
}
