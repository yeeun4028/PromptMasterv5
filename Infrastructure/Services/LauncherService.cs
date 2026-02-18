using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PromptMasterv5.Infrastructure.Services
{
    public class LauncherService : ILauncherService
    {
        private List<LauncherItem> _cache = new();

        public async Task<List<LauncherItem>> GetItemsAsync(IEnumerable<string> paths)
        {
            return await Task.Run(() =>
            {
                var items = new List<LauncherItem>();
                if (paths == null) return items;

                foreach (var path in paths)
                {
                    if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) continue;

                    try
                    {
                        // Get all files in the directory
                        var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(f => IsLaunchable(f));

                        foreach (var file in files)
                        {
                            items.Add(new LauncherItem
                            {
                                Title = Path.GetFileNameWithoutExtension(file),
                                FilePath = file,
                                IconPath = file // UI can use converter to display icon from path
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other paths
                        LoggerService.Instance.LogException(ex, $"Failed to scan directory during launcher discovery: {path}", "LauncherService.GetItemsAsync");
                    }
                }

                _cache = items;
                return items;
            });
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        private bool IsLaunchable(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return ext == ".exe" || ext == ".lnk" || ext == ".bat" || ext == ".cmd" || ext == ".ps1";
        }
    }
}
