using PromptMasterv5.Core.Models;

namespace PromptMasterv5.ViewModels.Messages
{
    public sealed record RequestMoveFileToFolderMessage(PromptItem File, FolderItem TargetFolder);
}

