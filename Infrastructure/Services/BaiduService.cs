using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PromptMasterv5.Infrastructure.Services
{
    public class BaiduService
    {
        private readonly HttpClient _client;

        public BaiduService()
        {
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> TranslateAsync(string appId, string secretKey, string text, string from = "auto", string to = "zh")
        {
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secretKey))
                return "请先在设置中配置百度翻译 AppID 和 密钥";

            if (string.IsNullOrWhiteSpace(text))
                return "翻译内容为空";

            try
            {
                string salt = DateTime.Now.Ticks.ToString();
                string rawSignStr = appId + text + salt + secretKey;
                string sign = EncryptString(rawSignStr);

                string url = $"https://fanyi-api.baidu.com/api/trans/vip/translate?q={Uri.EscapeDataString(text)}&from={from}&to={to}&appid={appId}&salt={salt}&sign={sign}";

                var response = await _client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                var jsonNode = JsonNode.Parse(json);
                if (jsonNode == null) return "翻译接口返回数据为空";

                var errorCode = jsonNode["error_code"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode) && errorCode != "52000")
                {
                    var errorMsg = jsonNode["error_msg"]?.ToString() ?? "未知错误";
                    return $"翻译失败 ({errorCode}): {errorMsg}";
                }

                if (jsonNode["trans_result"] is JsonArray transArray)
                {
                    var sb = new StringBuilder();
                    foreach (var item in transArray)
                    {
                        var dst = item?["dst"]?.ToString();
                        if (!string.IsNullOrEmpty(dst))
                        {
                            sb.AppendLine(dst);
                        }
                    }
                    return sb.ToString().Trim();
                }

                return $"未解析到翻译结果。原始内容：{json}";
            }
            catch (Exception ex)
            {
                return $"翻译异常：{ex.Message}";
            }
        }

        public async Task<string> OcrAsync(string apiKey, string secretKey, byte[] imageBytes)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
                return "请先在设置中配置百度 OCR API Key 和 Secret Key";

            try
            {
                string tokenUrl = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";
                var tokenResponse = await _client.PostAsync(tokenUrl, null);
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

                var tokenNode = JsonNode.Parse(tokenJson);
                var accessToken = tokenNode?["access_token"]?.ToString();

                if (string.IsNullOrEmpty(accessToken))
                    return "OCR 鉴权失败：无法获取 AccessToken，请检查 Key 是否正确";

                string ocrUrl = $"https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token={accessToken}";

                string base64 = Convert.ToBase64String(imageBytes);
                var postData = new List<KeyValuePair<string, string>>
                {
                    new("image", base64),
                    new("language_type", "CHN_ENG")
                };

                using var content = new FormUrlEncodedContent(postData);
                var response = await _client.PostAsync(ocrUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                var jsonNode = JsonNode.Parse(json);
                if (jsonNode == null) return "OCR 接口返回为空";

                if (jsonNode["error_code"] != null)
                {
                    return $"OCR 失败: {jsonNode["error_msg"]}";
                }

                if (jsonNode["words_result"] is JsonArray wordsArray)
                {
                    var sb = new StringBuilder();
                    foreach (var item in wordsArray)
                    {
                        sb.AppendLine(item?["words"]?.ToString());
                    }
                    return sb.ToString().Trim();
                }

                return "未识别到文字";
            }
            catch (Exception ex)
            {
                return $"OCR 异常: {ex.Message}";
            }
        }

        private static string EncryptString(string str)
        {
            using (var md5 = MD5.Create())
            {
                var byteOld = Encoding.UTF8.GetBytes(str);
                var byteNew = md5.ComputeHash(byteOld);
                var sb = new StringBuilder();
                foreach (var b in byteNew)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
