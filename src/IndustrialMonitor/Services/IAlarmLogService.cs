using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

public interface IAlarmLogService
{
    /// <summary>新增报警后广播（UI 线程）。</summary>
    event Action<AlarmRecord>? AlarmInserted;

    void Insert(AlarmRecord record);
    List<AlarmRecord> Query(string? type, string? deviceId, DateTime? from, DateTime? to, int page, int pageSize, out int total);
    int CountToday();
}
