namespace PromptMasterv5.Core.Interfaces
{
    public interface ICommandExecutionService
    {
        void LoadCommands();
        bool ExecuteCommand(string text);
    }
}
