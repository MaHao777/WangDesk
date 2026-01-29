using WangDesk.App.Models;

namespace WangDesk.App.Services;

/// <summary>
/// 系统监控服务接口
/// </summary>
public interface ISystemMonitorService
{
    /// <summary>
    /// 获取系统监控数据
    /// </summary>
    SystemMetrics GetMetrics();
    
    /// <summary>
    /// 获取存储设备信息列表
    /// </summary>
    List<StorageInfo> GetStorageInfo();
    
    /// <summary>
    /// 开始监控
    /// </summary>
    void StartMonitoring();
    
    /// <summary>
    /// 停止监控
    /// </summary>
    void StopMonitoring();
    
    /// <summary>
    /// 监控数据更新事件
    /// </summary>
    event EventHandler<SystemMetrics>? MetricsUpdated;
}
