using NAudio.Wave;
using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PromptMasterv5.Infrastructure.Services
{
    public class VoiceService : IVoiceService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly HttpClient _httpClient;
        
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _waveWriter;
        private string _tempFilePath = "";
        
        private bool _isRecording;
        public bool IsRecording => _isRecording;

        public event EventHandler<float>? OnAudioLevelChanged;
        public event EventHandler? OnRecordingStarted;
        public event EventHandler? OnRecordingStopped;

        private DateTime _lastAudioDetectedTime;
        private const float SilenceThreshold = 0.02f; // Adjust as needed
        private const int SilenceTimeoutMs = 1500; // 1.5 seconds of silence to auto-stop

        public VoiceService(ISettingsService settingsService, IHttpClientFactory httpClientFactory)
        {
            _settingsService = settingsService;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public void UpdateConfig(string baseUrl, string apiKey, string model)
        {
            // Config is pulled from _settingsService directly or passed in. 
            // If we use _settingsService, we might not need this method unless we cache values.
        }

        public void StartRecording()
        {
            if (_isRecording) return;

            try
            {
                _waveIn = new WaveInEvent();
                _waveIn.DeviceNumber = 0; // Default device
                _waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz mono (ideal for Whisper)
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;

                _tempFilePath = Path.Combine(Path.GetTempPath(), $"voice_cmd_{Guid.NewGuid()}.wav");
                _waveWriter = new WaveFileWriter(_tempFilePath, _waveIn.WaveFormat);

                _waveIn.StartRecording();
                _isRecording = true;
                _lastAudioDetectedTime = DateTime.Now;
                
                OnRecordingStarted?.Invoke(this, EventArgs.Empty);
                LoggerService.Instance.LogInfo("Voice recording started", "VoiceService.StartRecording");
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Failed to start recording", "VoiceService.StartRecording");
                CleanupResources();
            }
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_waveWriter == null) return;

            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

            // Calculate peak volume for level meter
            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i + 0]);
                var sample32 = sample / 32768f;
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            OnAudioLevelChanged?.Invoke(this, max);

            // Simple silence detection logic could go here if we want auto-stop
            // For now, relying on manual stop or advanced VAD later
        }

        private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            _isRecording = false;
            _waveWriter?.Dispose();
            _waveWriter = null;
            _waveIn?.Dispose();
            _waveIn = null;
            
            OnRecordingStopped?.Invoke(this, EventArgs.Empty);
        }

        public async Task<string> StopRecordingAndTranscribeAsync()
        {
            if (!_isRecording && string.IsNullOrEmpty(_tempFilePath)) return string.Empty;

            try
            {
                // Stop recording
                _waveIn?.StopRecording();
                
                // Wait briefly for the file to be fully written and closed (handled in RecordingStopped event, but we await completion)
                while (_isRecording)
                {
                    await Task.Delay(50);
                }

                if (!File.Exists(_tempFilePath)) return string.Empty;

                // Send to API
                return await TranscribeFileAsync(_tempFilePath);

            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Error during stop and prescribe", "VoiceService.StopRecordingAndTranscribeAsync");
                return ""; // Return empty on error
            }
            finally
            {
                // Cleanup file
                try
                {
                    if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
                }
                catch { }
                _tempFilePath = "";
            }
        }

        public void CancelRecording()
        {
            if (_isRecording)
            {
                _waveIn?.StopRecording();
            }
            // Cleanup file immediately
            try
            {
                if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
            }
            catch { }
            _tempFilePath = "";
        }

        private async Task<string> TranscribeFileAsync(string filePath)
        {
            try
            {
                var config = _settingsService.Config;
                string apiKey = config.VoiceApiKey;
                string baseUrl = config.VoiceApiBaseUrl;
                string model = config.VoiceApiModel;

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new Exception("Voice API Key is not configured.");

                // Ensure BaseURL ends correctly
                if (!baseUrl.EndsWith("/")) baseUrl += "/";
                // If the user provided the root (e.g. https://api.openai.com/v1), append 'audio/transcriptions'
                string requestUrl = baseUrl;
                if (!requestUrl.Contains("transcriptions"))
                {
                    requestUrl = new Uri(new Uri(baseUrl), "audio/transcriptions").ToString();
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using var form = new MultipartFormDataContent();
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var fileContent = new StreamContent(fileStream);
                
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
                form.Add(fileContent, "file", "audio.wav");
                form.Add(new StringContent(model), "model");

                request.Content = form;

                var response = await _httpClient.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API Error: {response.StatusCode} - {responseString}");
                }

                // Parse JSON response key "text"
                using var doc = System.Text.Json.JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString()?.Trim() ?? "";
                }
                
                return "";
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Transcription failed", "VoiceService.TranscribeFileAsync");
                throw;
            }
        }

        private void CleanupResources()
        {
            _waveWriter?.Dispose();
            _waveWriter = null;
            _waveIn?.Dispose();
            _waveIn = null;
            _isRecording = false;
        }

        public void Dispose()
        {
            CleanupResources();
            // _audioStream?.Dispose(); // Not used currently
        }
    }
}
