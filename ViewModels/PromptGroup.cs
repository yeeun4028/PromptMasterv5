
using System.Collections.Generic;
using PromptMasterv5.Core.Models;

namespace PromptMasterv5.ViewModels
{
    public class PromptGroup
    {
        public string FolderName { get; set; } = "";
        public IEnumerable<PromptItem> Prompts { get; set; } = new List<PromptItem>();
    }
}
