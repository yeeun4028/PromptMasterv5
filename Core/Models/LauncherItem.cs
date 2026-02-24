using System;

namespace PromptMasterv5.Core.Models
{
    public enum LauncherCategory
    {
        Bookmark,
        Application,
        Tool
    }

    public class LauncherItem
    {
        public string Title { get; set; } = string.Empty;
        public string? IconGeometry { get; set; }
        public string? IconPath { get; set; }
        public string? FilePath { get; set; }
        public Action? Action { get; set; }
        public bool RunAsAdmin { get; set; }
        public LauncherCategory Category { get; set; } = LauncherCategory.Application;
        public int DisplayOrder { get; set; }
    }
}
