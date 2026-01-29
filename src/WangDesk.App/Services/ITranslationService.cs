namespace WangDesk.App.Services;

/// <summary>
/// 翻译服务接口
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// 检测语言
    /// </summary>
    string DetectLanguage(string text);
    
    /// <summary>
    /// 翻译文本（自动检测语言并在中英文之间翻译）
    /// </summary>
    Task<string> TranslateAsync(string text);
    
    /// <summary>
    /// 配置API密钥
    /// </summary>
    void Configure(string appId, string secretKey);
    
    /// <summary>
    /// 检查是否已配置
    /// </summary>
    bool IsConfigured { get; }
}
