using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WangDesk.App.Models;

/// <summary>
/// 系统监控数据模型
/// </summary>
public class SystemMetrics : INotifyPropertyChanged
{
    private double _cpuUsage;
    private double _cpuUserUsage;
    private double _cpuKernelUsage;
    private double _cpuAvailable;
    private double _memoryUsagePercent;
    private double _memoryTotalGB;
    private double _memoryUsedGB;
    private double _memoryAvailableGB;
    private string _networkSent = "0 B/s";
    private string _networkReceived = "0 B/s";

    public double CpuUsage
    {
        get => _cpuUsage;
        set => SetProperty(ref _cpuUsage, value);
    }

    public double CpuUserUsage
    {
        get => _cpuUserUsage;
        set => SetProperty(ref _cpuUserUsage, value);
    }

    public double CpuKernelUsage
    {
        get => _cpuKernelUsage;
        set => SetProperty(ref _cpuKernelUsage, value);
    }

    public double CpuAvailable
    {
        get => _cpuAvailable;
        set => SetProperty(ref _cpuAvailable, value);
    }

    public double MemoryUsagePercent
    {
        get => _memoryUsagePercent;
        set => SetProperty(ref _memoryUsagePercent, value);
    }

    public double MemoryTotalGB
    {
        get => _memoryTotalGB;
        set => SetProperty(ref _memoryTotalGB, value);
    }

    public double MemoryUsedGB
    {
        get => _memoryUsedGB;
        set => SetProperty(ref _memoryUsedGB, value);
    }

    public double MemoryAvailableGB
    {
        get => _memoryAvailableGB;
        set => SetProperty(ref _memoryAvailableGB, value);
    }

    public string NetworkSent
    {
        get => _networkSent;
        set => SetProperty(ref _networkSent, value);
    }

    public string NetworkReceived
    {
        get => _networkReceived;
        set => SetProperty(ref _networkReceived, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null!)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
