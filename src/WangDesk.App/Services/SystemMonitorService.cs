using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WangDesk.App.Models;

namespace WangDesk.App.Services;

/// <summary>
/// 系统监控服务实现
/// </summary>
public class SystemMonitorService : ISystemMonitorService, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _cpuUserCounter;
    private readonly PerformanceCounter _cpuKernelCounter;
    private long _lastBytesSent;
    private long _lastBytesReceived;
    private DateTime _lastNetworkCheck;
    private readonly object _lockObject = new();
    private PerformanceCounter[]? _gpuCounters;
    private DateTime _lastGpuCounterRefresh = DateTime.MinValue;

    public event EventHandler<SystemMetrics>? MetricsUpdated;

    public SystemMonitorService()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _cpuUserCounter = new PerformanceCounter("Processor", "% User Time", "_Total");
        _cpuKernelCounter = new PerformanceCounter("Processor", "% Privileged Time", "_Total");
        
        // 初始化网络计数器
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up 
                      && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        
        foreach (var ni in interfaces)
        {
            var stats = ni.GetIPv4Statistics();
            _lastBytesSent += stats.BytesSent;
            _lastBytesReceived += stats.BytesReceived;
        }
        _lastNetworkCheck = DateTime.Now;
        
        // 预热计数器
        _cpuCounter.NextValue();
        _cpuUserCounter.NextValue();
        _cpuKernelCounter.NextValue();
    }

    public void StartMonitoring()
    {
        if (_monitoringTask != null) return;

        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var metrics = GetMetrics();
                    MetricsUpdated?.Invoke(this, metrics);
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }, _cancellationTokenSource.Token);
    }

    public void StopMonitoring()
    {
        _cancellationTokenSource?.Cancel();
        _monitoringTask?.Wait(TimeSpan.FromSeconds(2));
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _monitoringTask = null;
    }

    public SystemMetrics GetMetrics()
    {
        var metrics = new SystemMetrics();

        try
        {
            // CPU 信息
            metrics.CpuUsage = Math.Round(_cpuCounter.NextValue(), 1);
            metrics.CpuUserUsage = Math.Round(_cpuUserCounter.NextValue(), 1);
            metrics.CpuKernelUsage = Math.Round(_cpuKernelCounter.NextValue(), 1);
            metrics.CpuAvailable = Math.Round(100 - metrics.CpuUsage, 1);

            // 内存信息
            var memoryStatus = GetMemoryStatus();
            metrics.MemoryTotalGB = Math.Round(memoryStatus.TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0), 2);
            metrics.MemoryAvailableGB = Math.Round(memoryStatus.AvailablePhysical / (1024.0 * 1024.0 * 1024.0), 2);
            metrics.MemoryUsedGB = Math.Round(metrics.MemoryTotalGB - metrics.MemoryAvailableGB, 2);
            metrics.MemoryUsagePercent = Math.Round((metrics.MemoryUsedGB / metrics.MemoryTotalGB) * 100, 1);

            // 网络信息
            var (sentSpeed, receivedSpeed) = GetNetworkSpeed();
            metrics.NetworkSent = sentSpeed;
            metrics.NetworkReceived = receivedSpeed;

            // GPU 信息
            metrics.GpuUsage = Math.Round(GetGpuUsage(), 1);
        }
        catch (Exception)
        {
            // 记录日志或处理异常
        }

        return metrics;
    }

    public List<StorageInfo> GetStorageInfo()
    {
        var storageList = new List<StorageInfo>();

        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var info = new StorageInfo
                {
                    DriveLetter = drive.Name.Substring(0, 1),
                    DriveName = drive.VolumeLabel ?? $"{drive.Name.Substring(0, 1)} Drive",
                    TotalGB = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2),
                    AvailableGB = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2),
                    UsedGB = Math.Round((drive.TotalSize - drive.AvailableFreeSpace) / (1024.0 * 1024.0 * 1024.0), 2)
                };
                info.UsagePercent = Math.Round((info.UsedGB / info.TotalGB) * 100, 1);
                storageList.Add(info);
            }
        }
        catch (Exception)
        {
            // 记录日志或处理异常
        }

        return storageList;
    }

    private (long TotalPhysicalBytes, long AvailablePhysical) GetMemoryStatus()
    {
        var status = new MEMORYSTATUSEX();
        status.dwLength = (uint)Marshal.SizeOf(status);
        GlobalMemoryStatusEx(ref status);

        return (TotalPhysicalBytes: (long)status.ullTotalPhys,
                AvailablePhysical: (long)status.ullAvailPhys);
    }

    private (string SentSpeed, string ReceivedSpeed) GetNetworkSpeed()
    {
        long currentBytesSent = 0;
        long currentBytesReceived = 0;

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up 
                      && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

        foreach (var ni in interfaces)
        {
            var stats = ni.GetIPv4Statistics();
            currentBytesSent += stats.BytesSent;
            currentBytesReceived += stats.BytesReceived;
        }

        var now = DateTime.Now;
        var timeSpan = now - _lastNetworkCheck;
        
        if (timeSpan.TotalSeconds > 0)
        {
            var sentDiff = currentBytesSent - _lastBytesSent;
            var receivedDiff = currentBytesReceived - _lastBytesReceived;
            var sentSpeed = sentDiff / timeSpan.TotalSeconds;
            var receivedSpeed = receivedDiff / timeSpan.TotalSeconds;

            _lastBytesSent = currentBytesSent;
            _lastBytesReceived = currentBytesReceived;
            _lastNetworkCheck = now;

            return (FormatBytesSpeed(sentSpeed), FormatBytesSpeed(receivedSpeed));
        }

        return ("0 B/s", "0 B/s");
    }

    private static string FormatBytesSpeed(double bytesPerSecond)
    {
        const double KB = 1024;
        const double MB = KB * 1024;
        const double GB = MB * 1024;

        if (bytesPerSecond >= GB)
            return $"{bytesPerSecond / GB:F2} GB/s";
        if (bytesPerSecond >= MB)
            return $"{bytesPerSecond / MB:F2} MB/s";
        if (bytesPerSecond >= KB)
            return $"{bytesPerSecond / KB:F2} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }

    public void Dispose()
    {
        StopMonitoring();
        _cpuCounter?.Dispose();
        _cpuUserCounter?.Dispose();
        _cpuKernelCounter?.Dispose();
        _cancellationTokenSource?.Dispose();
        DisposeGpuCounters();
    }

    private double GetGpuUsage()
    {
        try
        {
            // 每 5 秒刷新一次 GPU 计数器实例列表（因为进程会变化）
            if (_gpuCounters == null || (DateTime.Now - _lastGpuCounterRefresh).TotalSeconds > 5)
            {
                RefreshGpuCounters();
            }

            if (_gpuCounters == null || _gpuCounters.Length == 0)
                return 0;

            double total = 0;
            foreach (var counter in _gpuCounters)
            {
                try
                {
                    total += counter.NextValue();
                }
                catch
                {
                    // 某些实例可能已失效，忽略
                }
            }

            return Math.Min(total, 100);
        }
        catch
        {
            return 0;
        }
    }

    private void RefreshGpuCounters()
    {
        try
        {
            DisposeGpuCounters();

            var category = new PerformanceCounterCategory("GPU Engine");
            var instances = category.GetInstanceNames();

            // 只取包含 engtype_3D 的实例（3D 引擎最能反映 GPU 负载）
            var engineInstances = instances
                .Where(name => name.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var counters = new List<PerformanceCounter>();
            foreach (var instance in engineInstances)
            {
                try
                {
                    var pc = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                    pc.NextValue(); // 预热
                    counters.Add(pc);
                }
                catch
                {
                    // 忽略无效实例
                }
            }

            _gpuCounters = counters.ToArray();
            _lastGpuCounterRefresh = DateTime.Now;
        }
        catch
        {
            _gpuCounters = Array.Empty<PerformanceCounter>();
            _lastGpuCounterRefresh = DateTime.Now;
        }
    }

    private void DisposeGpuCounters()
    {
        if (_gpuCounters != null)
        {
            foreach (var c in _gpuCounters)
            {
                try { c.Dispose(); } catch { }
            }
            _gpuCounters = null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
