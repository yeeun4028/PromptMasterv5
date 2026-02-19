using PromptMasterv5.Core.Interfaces;
using PromptMasterv5.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PromptMasterv5.Infrastructure.Services
{
    public class CommandExecutionService : ICommandExecutionService
    {
        private readonly ISettingsService _settingsService;
        private Dictionary<string, string> _commands = new();

        public CommandExecutionService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            LoadCommands();
        }

        public void LoadCommands()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settingsService.Config.VoiceCommandConfigPath);
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    _commands = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    LoggerService.Instance.LogInfo($"Loaded {_commands.Count} voice commands", "CommandExecutionService.LoadCommands");
                }
                else
                {
                    LoggerService.Instance.LogWarning("Voice command config file not found", "CommandExecutionService.LoadCommands");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogException(ex, "Failed to load voice commands", "CommandExecutionService.LoadCommands");
            }
        }

        public bool ExecuteCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            // Normalize text: remove punctuation, lower case
            var normalizedText = NormalizeText(text);

            LoggerService.Instance.LogInfo($"Processing voice command: '{text}' -> '{normalizedText}'", "CommandExecutionService.ExecuteCommand");

            // 1. Exact match
            if (_commands.TryGetValue(normalizedText, out var command))
            {
                return ExecuteProcess(command);
            }

            // 2. Fuzzy match / Contains
            // Find the best match where the command key is contained in the spoken text or vice versa.
            // For simple implementation, check if spoken text contains the key
            var match = _commands.Keys.FirstOrDefault(k => normalizedText.Contains(NormalizeText(k)));
            if (match != null)
            {
                return ExecuteProcess(_commands[match]);
            }

            return false;
        }

        private bool ExecuteProcess(string command)
        {
            try
            {
                ProcessStartInfo psi;

                if (command.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    // PowerShell scripts need to be launched via powershell.exe
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -File \"{command}\"",
                        UseShellExecute = false
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = command,
                        UseShellExecute = true
                    };
                }

                Process.Start(psi);
                LoggerService.Instance.LogInfo($"Executed voice command: {command}", "CommandExecutionService.ExecuteProcess");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogException(ex, $"Failed to execute command: {command}", "CommandExecutionService.ExecuteProcess");
                return false;
            }
        }

        private string NormalizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return new string(input.Where(c => !char.IsPunctuation(c)).ToArray()).ToLowerInvariant().Trim();
        }
    }
}
