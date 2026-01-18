using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PromptMasterv5.Core.Models;
using PromptMasterv5.Core.Interfaces;

namespace PromptMasterv5.Infrastructure.Services
{
    public class FabricService
    {
        private readonly string _patternsPath;
        private List<string> _cachedPatternNames = new();

        public FabricService()
        {
            _patternsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "patterns");
            RefreshPatterns();
        }

        public void RefreshPatterns()
        {
            if (Directory.Exists(_patternsPath))
            {
                _cachedPatternNames = Directory.GetDirectories(_patternsPath)
                                               .Select(Path.GetFileName)
                                               .Where(n => !string.IsNullOrEmpty(n))
                                               .ToList()!;
            }
        }

        public async Task<string> FindBestPatternAndContentAsync(string userInput, IAiService aiService, AppConfig config)
        {
            if (_cachedPatternNames.Count == 0)
            {
                return $"[错误] 未在 {_patternsPath} 下找到任何模式文件夹。请确认已下载 Fabric patterns。";
            }

            string patternList = string.Join(", ", _cachedPatternNames);

            if (patternList.Length > 10000) patternList = patternList.Substring(0, 10000) + "...";

            string routerSystemPrompt = $@"You are a semantic router. 
Your task is to select BEST matching pattern name from following list based on user's input.
PATTERN LIST: [{patternList}]

Rules:
1. Return ONLY the pattern name. Do not add any explanation or punctuation.
2. If no pattern matches well, return 'default'.";

            string selectedPatternName = await aiService.ChatAsync(userInput, config, routerSystemPrompt);

            selectedPatternName = selectedPatternName.Trim().TrimEnd('.').TrimEnd('。');

            if (string.Equals(selectedPatternName, "default", StringComparison.OrdinalIgnoreCase) ||
                !_cachedPatternNames.Contains(selectedPatternName))
            {
                return "";
            }

            string systemMdPath = Path.Combine(_patternsPath, selectedPatternName, "system.md");
            if (File.Exists(systemMdPath))
            {
                string content = await File.ReadAllTextAsync(systemMdPath);
                return content + "\n\n# OUTPUT INSTRUCTIONS\nIMPORTANT: Please output the final response in Simplified Chinese unless the user explicitly asks for another language.";
            }

            return "";
        }
    }
}
