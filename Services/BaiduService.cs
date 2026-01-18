using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes; // 必须引用: 用于解析 JSON
using System.Threading.Tasks;

namespace PromptMasterv5.Services
{
    public class BaiduService
    {
        // 保持 HttpClient 单例，避免重复创建连接导致端口耗尽
        private readonly HttpClient _client;

        public BaiduService()
        {
            _client = new HttpClient();
            // 设置超时，防止网络卡死
            _client.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 文本翻译 (完美修复版)
        /// 采用 POST 方式提交，自动处理 URL 编码，支持长文本和特殊字符
        /// </summary>
        /// <param name="appId">百度翻译 AppID</param>
        /// <param name="secretKey">密钥</param>
        /// <param name="q">待翻译文本</param>
        /// <param name="to">目标语言 (默认 zh)</param>
        /// <returns>翻译结果或错误提示</returns>
        public async Task<string> TranslateAsync(string appId, string secretKey, string q, string to = "zh")
        {
            // 1. 基础检查
            if (string.IsNullOrWhiteSpace(q)) return "";
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secretKey))
                return "请先在设置中配置百度翻译 AppID 和密钥";

            try
            {
                string url = "https://api.fanyi.baidu.com/api/trans/vip/translate";
                string salt = DateTime.Now.Ticks.ToString();
                string from = "auto";

                // 2. 签名计算 (核心点：Sign 必须使用【未编码】的原始字符串计算)
                // 官方公式: appid + q + salt + 密钥
                string signStr = appId + q + salt + secretKey;
                string sign = MD5Encrypt(signStr);

                // 3. 构建请求参数 (使用 FormUrlEncodedContent 自动处理特殊字符编码)
                var postData = new List<KeyValuePair<string, string>>
                {
                    new("q", q),
                    new("from", from),
                    new("to", to),
                    new("appid", appId),
                    new("salt", salt),
                    new("sign", sign)
                };

                using var content = new FormUrlEncodedContent(postData);

                // 4. 发送 POST 请求
                var response = await _client.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();

                // 5. 安全解析结果
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode == null) return "错误：接口返回数据为空";

                // 优先检查错误码 (52000 为成功)
                var errorCode = jsonNode["error_code"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode) && errorCode != "52000")
                {
                    var errorMsg = jsonNode["error_msg"]?.ToString() ?? "未知错误";
                    return $"翻译失败 ({errorCode}): {errorMsg}";
                }

                // 提取翻译结果 (处理多段文本)
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

        /// <summary>
        /// 通用文字识别 (OCR) - 标准版
        /// </summary>
        public async Task<string> OcrAsync(string apiKey, string secretKey, byte[] imageBytes)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
                return "请先在设置中配置百度 OCR API Key 和 Secret Key";

            try
            {
                // 1. 获取 Access Token
                // 注意：OCR 的鉴权方式与翻译不同，需要先换取 Token
                string tokenUrl = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";

                var tokenResponse = await _client.PostAsync(tokenUrl, null);
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenNode = JsonNode.Parse(tokenJson);
                var accessToken = tokenNode?["access_token"]?.ToString();

                if (string.IsNullOrEmpty(accessToken))
                    return "OCR 鉴权失败：无法获取 AccessToken，请检查 Key 是否正确";

                // 2. 调用 OCR 接口
                string ocrUrl = $"https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token={accessToken}";
                string base64 = Convert.ToBase64String(imageBytes);

                var postData = new List<KeyValuePair<string, string>>
                {
                    new("image", base64),
                    new("language_type", "CHN_ENG") // 识别中英混合
                };
                using var content = new FormUrlEncodedContent(postData);

                var response = await _client.PostAsync(ocrUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                // 3. 解析结果
                var resultNode = JsonNode.Parse(json);
                if (resultNode == null) return "错误：OCR 返回数据为空";

                // 检查错误
                var errorMsg = resultNode["error_msg"]?.ToString();
                if (!string.IsNullOrEmpty(errorMsg)) return $"OCR 识别错误：{errorMsg}";

                var wordsResult = resultNode["words_result"] as JsonArray;
                if (wordsResult == null || wordsResult.Count == 0) return "未识别到文字";

                var sb = new StringBuilder();
                foreach (var item in wordsResult)
                {
                    var words = item?["words"]?.ToString();
                    if (!string.IsNullOrEmpty(words))
                    {
                        sb.AppendLine(words);
                    }
                }
                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                return $"OCR 异常：{ex.Message}";
            }
        }

        /// <summary>
        /// MD5 加密辅助方法
        /// </summary>
        private string MD5Encrypt(string str)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(str);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // 使用小写 x2 格式
            }
            return sb.ToString();
        }
    }
}