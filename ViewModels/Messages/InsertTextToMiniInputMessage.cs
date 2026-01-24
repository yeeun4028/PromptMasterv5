using CommunityToolkit.Mvvm.Messaging.Messages;

namespace PromptMasterv5.ViewModels.Messages
{
    public class InsertTextToMiniInputMessage : ValueChangedMessage<string>
    {
        public InsertTextToMiniInputMessage(string value) : base(value)
        {
        }
    }
}
