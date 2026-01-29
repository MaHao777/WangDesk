namespace WangDesk.App.Services;

/// <summary>
/// 开机自启服务接口
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// 是否已设置为开机自启
    /// </summary>
    bool IsAutoStartEnabled();
    
    /// <summary>
    /// 设置开机自启
    /// </summary>
    void SetAutoStart(bool enable);
}
