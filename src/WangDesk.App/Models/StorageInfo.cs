namespace WangDesk.App.Models;

/// <summary>
/// 存储信息模型
/// </summary>
public class StorageInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public string DriveName { get; set; } = string.Empty;
    public double UsagePercent { get; set; }
    public double UsedGB { get; set; }
    public double AvailableGB { get; set; }
    public double TotalGB { get; set; }
}
