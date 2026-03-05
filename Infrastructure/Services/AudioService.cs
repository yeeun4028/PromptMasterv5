using PromptMasterv5.Core.Interfaces;
using System.Threading.Tasks;

namespace PromptMasterv5.Infrastructure.Services
{
    public class AudioService : IAudioService
    {
        public Task PlayShutterSoundAsync() => Task.CompletedTask;

        public Task PlaySuccessSoundAsync() => Task.CompletedTask;

        public Task PlayErrorSoundAsync() => Task.CompletedTask;
    }
}
