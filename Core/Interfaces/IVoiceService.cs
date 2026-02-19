using System;
using System.Threading.Tasks;

namespace PromptMasterv5.Core.Interfaces
{
    public interface IVoiceService
    {
        bool IsRecording { get; }
        event EventHandler<float> OnAudioLevelChanged; // For visualization
        event EventHandler OnRecordingStarted;
        event EventHandler OnRecordingStopped;

        void StartRecording();
        Task<string> StopRecordingAndTranscribeAsync(IReadOnlyList<string>? hotwords = null);
        void CancelRecording();
        
        // Configuration updates
        void UpdateConfig(string baseUrl, string apiKey, string model);
    }
}
