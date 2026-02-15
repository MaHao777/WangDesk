namespace WangDesk.App.Models;

/// <summary>
/// 自定义提醒音频条目
/// </summary>
public class CustomReminderSoundItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
}
