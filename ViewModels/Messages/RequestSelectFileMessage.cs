using PromptMasterv5.Core.Models;

namespace PromptMasterv5.ViewModels.Messages
{
    public sealed record RequestSelectFileMessage(PromptItem? File, bool EnterEditMode);
}

