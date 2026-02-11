using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using PromptMasterv5.Core.Models;
using PromptMasterv5.Core.Interfaces;
using System.Net.Http;
using System.Threading;

namespace PromptMasterv5.Infrastructure.Services
{
    public class AiService : IAiService
    {
        public Task<string> ChatAsync(string userContent, AppConfig config, string? systemPrompt = null)
        {
            return ChatAsync(userContent, config.AiApiKey, config.AiBaseUrl, config.AiModel, systemPrompt);
        }

        public async Task<string> ChatAsync(string userContent, string apiKey, string baseUrl, string model, string? systemPrompt = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "[设置错误] 请先在设置中配置 API Key";
            }

            var openAiService = CreateOpenAiService(apiKey, baseUrl);

            string finalSystemPrompt = systemPrompt ?? "You are a helpful assistant. Output result directly without unnecessary conversational filler. IMPORTANT: Always answer in Simplified Chinese unless the user explicitly asks for another language.";

            var messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(finalSystemPrompt),
                ChatMessage.FromUser(userContent)
            };

            var request = new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = model,
                Temperature = 0.7f
            };

            try
            {
                var completionResult = await openAiService.ChatCompletion.CreateCompletion(request);

                if (completionResult.Successful)
                {
                    var choice = completionResult.Choices?.FirstOrDefault();
                    if (choice != null && choice.Message != null && !string.IsNullOrEmpty(choice.Message.Content))
                    {
                        return choice.Message.Content.Trim();
                    }
                    return "[AI 无响应] 返回内容为空";
                }
                else
                {
                    if (completionResult.Error == null) return "[AI 错误] 未知网络错误";
                    return $"[AI 错误] {completionResult.Error.Message} ({completionResult.Error.Type})";
                }
            }
            catch (Exception ex)
            {
                return $"[系统错误] {ex.Message}";
            }
        }

        public async Task<string> ChatWithImageAsync(byte[] imageBytes, string apiKey, string baseUrl, string model, string? systemPrompt = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return "[设置错误] 请先配置 API Key";
            if (imageBytes == null || imageBytes.Length == 0) return "[输入错误] 图片数据为空";

            var openAiService = CreateOpenAiService(apiKey, baseUrl);

            string finalSystemPrompt = systemPrompt ?? "You are a helpful assistant. Please perform OCR on the provided image.";
            string base64Image = Convert.ToBase64String(imageBytes);
            string imageUrl = $"data:image/jpeg;base64,{base64Image}";

            var messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(finalSystemPrompt),
                ChatMessage.FromUser(
                    new List<MessageContent>
                    {
                        MessageContent.ImageUrlContent(imageUrl),
                        MessageContent.TextContent("Please identify all text in this image and output it directly.")
                    })
            };

            var request = new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = model,
                Temperature = 0.3f, // Lower temperature for OCR accuracy
                MaxTokens = 2000
            };

            try
            {
                var completionResult = await openAiService.ChatCompletion.CreateCompletion(request);
                if (completionResult.Successful)
                {
                    var choice = completionResult.Choices?.FirstOrDefault();
                    if (choice != null && choice.Message != null && !string.IsNullOrEmpty(choice.Message.Content))
                    {
                        return choice.Message.Content.Trim();
                    }
                    return "[AI 无响应] 返回内容为空";
                }
                else
                {
                    if (completionResult.Error == null) return "[AI 错误] 未知网络错误";
                    return $"[AI 错误] {completionResult.Error.Message} ({completionResult.Error.Type})";
                }
            }
            catch (Exception ex)
            {
                return $"[系统错误] {ex.Message}";
            }
        }

        public IAsyncEnumerable<string> ChatStreamAsync(string userContent, AppConfig config, string? systemPrompt = null)
        {
            return ChatStreamAsync(userContent, config.AiApiKey, config.AiBaseUrl, config.AiModel, systemPrompt);
        }

        public async IAsyncEnumerable<string> ChatStreamAsync(string userContent, string apiKey, string baseUrl, string model, string? systemPrompt = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                yield return "[设置错误] 请先在设置中配置 API Key";
                yield break;
            }

            var openAiService = CreateOpenAiService(apiKey, baseUrl);

            string finalSystemPrompt = systemPrompt ?? "You are a helpful assistant. Output result directly without unnecessary conversational filler. IMPORTANT: Always answer in Simplified Chinese unless the user explicitly asks for another language.";

            var messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(finalSystemPrompt),
                ChatMessage.FromUser(userContent)
            };

            var request = new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = model,
                Temperature = 0.7f,
                Stream = true // Enable streaming
            };

            await foreach (var completionResult in openAiService.ChatCompletion.CreateCompletionAsStream(request))
            {
                if (completionResult.Successful)
                {
                    var choice = completionResult.Choices?.FirstOrDefault();
                    if (choice != null && choice.Message != null && !string.IsNullOrEmpty(choice.Message.Content))
                    {
                        yield return choice.Message.Content;
                    }
                }
                else
                {
                    if (completionResult.Error != null)
                    {
                        var errorMsg = $"[AI 错误] {completionResult.Error.Message} (Type: {completionResult.Error.Type}, Code: {completionResult.Error.Code})";
                        LoggerService.Instance.LogError($"Stream Error: {errorMsg}", "AiService.ChatStreamAsync");
                        yield return errorMsg;
                    }
                    else
                    {
                         LoggerService.Instance.LogError("Stream Error: Unknown error (Successful=false, Error=null)", "AiService.ChatStreamAsync");
                         yield return "[AI 错误] 未知错误";
                    }
                }
            }
        }

        public IAsyncEnumerable<string> ChatStreamAsync(List<ChatMessage> messages, AppConfig config)
        {
            return ChatStreamAsync(messages, config.AiApiKey, config.AiBaseUrl, config.AiModel);
        }

        public async IAsyncEnumerable<string> ChatStreamAsync(List<ChatMessage> messages, string apiKey, string baseUrl, string model)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                yield return "[设置错误] 请先在设置中配置 API Key";
                yield break;
            }

            var openAiService = CreateOpenAiService(apiKey, baseUrl);

            var request = new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = model,
                Temperature = 0.7f,
                Stream = true
            };

            await foreach (var completionResult in openAiService.ChatCompletion.CreateCompletionAsStream(request))
            {
                if (completionResult.Successful)
                {
                    var choice = completionResult.Choices?.FirstOrDefault();
                    if (choice != null && choice.Message != null && !string.IsNullOrEmpty(choice.Message.Content))
                    {
                        yield return choice.Message.Content;
                    }
                }
                else
                {
                    if (completionResult.Error != null)
                    {
                        var errorMsg = $"[AI 错误] {completionResult.Error.Message} (Type: {completionResult.Error.Type}, Code: {completionResult.Error.Code})";
                        LoggerService.Instance.LogError($"Stream Error: {errorMsg}", "AiService.ChatStreamAsync");
                        yield return errorMsg;
                    }
                    else
                    {
                         LoggerService.Instance.LogError("Stream Error: Unknown error (Successful=false, Error=null)", "AiService.ChatStreamAsync");
                         yield return "[AI 错误] 未知错误";
                    }
                }
            }
        }

        public Task<(bool Success, string Message)> TestConnectionAsync(AppConfig config)
        {
            return TestConnectionAsync(config.AiApiKey, config.AiBaseUrl, config.AiModel);
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(string apiKey, string baseUrl, string model)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return (false, "API Key 为空");
            if (string.IsNullOrWhiteSpace(baseUrl))
                return (false, "API 地址为空");
            if (string.IsNullOrWhiteSpace(model))
                return (false, "模型名称为空");

            try
            {
                var openAiService = CreateOpenAiService(apiKey, baseUrl);

                var request = new ChatCompletionCreateRequest
                {
                    Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem("Test"),
                        ChatMessage.FromUser("Hi")
                    },
                    Model = model,
                    MaxTokens = 5
                };

                var completionResult = await openAiService.ChatCompletion.CreateCompletion(request);

                if (completionResult.Successful)
                {
                    return (true, "连接成功！");
                }
                else
                {
                    return (false, $"连接失败: {completionResult.Error?.Message ?? "未知错误"}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"连接异常: {ex.Message}");
            }
        }
        private OpenAIService CreateOpenAiService(string apiKey, string baseUrl)
        {
            var options = new OpenAiOptions
            {
                ApiKey = apiKey,
                BaseDomain = baseUrl
            };

            // 使用自定义 Handler 处理智谱 AI 的 URL 兼容性问题
            // 智谱 API 虽然号称兼容 OpenAI，但 path 使用 /api/paas/v4/
            // 标准 SDK 会自动追加 /v1/chat/completions，导致路径变为 /api/paas/v4/v1/... (404)
            var httpClient = new HttpClient(new ZhipuCompatHandler(new HttpClientHandler()))
            {
                Timeout = TimeSpan.FromMinutes(2) // 增加超时时间以防万一
            };

            return new OpenAIService(options, httpClient);
        }

        /// <summary>
        /// 智谱 AI URL 兼容性处理器
        /// </summary>
        private class ZhipuCompatHandler : DelegatingHandler
        {
            public ZhipuCompatHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri != null && request.RequestUri.Host.Contains("bigmodel.cn", StringComparison.OrdinalIgnoreCase))
                {
                    var uriStr = request.RequestUri.AbsoluteUri;
                    
                    // 1. 处理 SDK 自动追加 /v1/ 导致路径错误 (如 .../v4/v1/...)
                    // 同时处理可能的双斜杠问题 (.../v4//v1/...)
                    if (uriStr.Contains("/v4/v1/") || uriStr.Contains("/v4//v1/"))
                    {
                        var originalUri = uriStr;
                        
                        // 替换错误的 v1 路径，修正为 v4 直接衔接后续路径
                        var newUriStr = uriStr.Replace("/v4//v1/", "/v4/").Replace("/v4/v1/", "/v4/");
                        
                        request.RequestUri = new Uri(newUriStr);
                        
                        // Log the fix for debugging
                        LoggerService.Instance.LogInfo($"[ZhipuFix] Rewrote URL from {originalUri} to {newUriStr}", "AiService.ZhipuCompatHandler");
                    }
                    else
                    {
                         // Log normal requests to verify host match
                         // LoggerService.Instance.LogInfo($"[ZhipuCheck] URL pass-through: {uriStr}", "AiService.ZhipuCompatHandler");
                    }
                }
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
