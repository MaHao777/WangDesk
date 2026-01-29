using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace WangDesk.App.Services;

/// <summary>
/// 百度翻译服务实现
/// </summary>
public class BaiduTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private string _appId = string.Empty;
    private string _secretKey = string.Empty;
    private const string ApiUrl = "https://fanyi-api.baidu.com/api/trans/vip/translate";

    public bool IsConfigured => !string.IsNullOrEmpty(_appId) && !string.IsNullOrEmpty(_secretKey);

    public BaiduTranslationService()
    {
        _httpClient = new HttpClient();
    }

    public void Configure(string appId, string secretKey)
    {
        _appId = appId;
        _secretKey = secretKey;
    }

    public string DetectLanguage(string text)
    {
        // 简单检测：如果包含中文字符则认为是中文
        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                return "zh";
            }
        }
        return "en";
    }

    public async Task<string> TranslateAsync(string text)
    {
        if (!IsConfigured)
        {
            return "请先配置百度翻译API密钥";
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        try
        {
            var from = DetectLanguage(text);
            var to = from == "zh" ? "en" : "zh";
            
            var salt = new Random().Next(10000, 99999).ToString();
            var sign = CalculateSign(text, salt);

            var parameters = new Dictionary<string, string>
            {
                ["q"] = text,
                ["from"] = from,
                ["to"] = to,
                ["appid"] = _appId,
                ["salt"] = salt,
                ["sign"] = sign
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(ApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"翻译失败: HTTP {response.StatusCode}";
            }

            var result = JsonSerializer.Deserialize<BaiduTranslateResponse>(responseContent);
            
            if (result?.ErrorCode != null)
            {
                return $"翻译错误: {result.ErrorMsg} (错误码: {result.ErrorCode})";
            }

            if (result?.TransResult != null && result.TransResult.Count > 0)
            {
                return string.Join("\n", result.TransResult.ConvertAll(r => r.Dst));
            }

            return "翻译失败：未获取到结果";
        }
        catch (Exception ex)
        {
            return $"翻译异常: {ex.Message}";
        }
    }

    private string CalculateSign(string query, string salt)
    {
        var signStr = $"{_appId}{query}{salt}{_secretKey}";
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private class BaiduTranslateResponse
    {
        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("to")]
        public string? To { get; set; }

        [JsonPropertyName("trans_result")]
        public List<TranslateResult>? TransResult { get; set; }

        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string? ErrorMsg { get; set; }
    }

    private class TranslateResult
    {
        [JsonPropertyName("src")]
        public string? Src { get; set; }

        [JsonPropertyName("dst")]
        public string? Dst { get; set; }
    }
}
