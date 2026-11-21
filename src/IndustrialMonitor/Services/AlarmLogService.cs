using System.IO;
using System.Windows;
using System.Windows.Threading;
using IndustrialMonitor.Models;
using Microsoft.Data.Sqlite;

namespace IndustrialMonitor.Services;

/// <summary>SQLite 报警日志存储实现，含分页和筛选。</summary>
public sealed class AlarmLogService : IAlarmLogService
{
    private readonly string _dbPath;

    /// <summary>新增报警后广播（用于报警中心/设备页实时刷新），事件在 UI 线程触发。</summary>
    public event Action<AlarmRecord>? AlarmInserted;

    public AlarmLogService()
    {
        _dbPath = Path.Combine(AppContext.BaseDirectory, "users.db");
        // 表由 AuthService 统一创建，这里兜底
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS AlarmLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DeviceId TEXT NOT NULL,
                Type TEXT NOT NULL,
                Value REAL NOT NULL,
                Threshold REAL NOT NULL,
                Message TEXT NOT NULL,
                Time TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    public void Insert(AlarmRecord record)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AlarmLogs (DeviceId, Type, Value, Threshold, Message, Time)
            VALUES (@d, @t, @v, @th, @m, @time);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@d", record.DeviceId);
        cmd.Parameters.AddWithValue("@t", record.Type);
        cmd.Parameters.AddWithValue("@v", record.Value);
        cmd.Parameters.AddWithValue("@th", record.Threshold);
        cmd.Parameters.AddWithValue("@m", record.Message);
        cmd.Parameters.AddWithValue("@time", record.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        record.Id = Convert.ToInt64(cmd.ExecuteScalar()!);

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.BeginInvoke(() => AlarmInserted?.Invoke(record));
    }

    public List<AlarmRecord> Query(string? type, string? deviceId, DateTime? from, DateTime? to,
        int page, int pageSize, out int total)
    {
        var where = new List<string>();
        var paramValues = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(type) && type != "全部")
        {
            where.Add("Type = @type");
            paramValues["@type"] = type;
        }
        if (!string.IsNullOrEmpty(deviceId) && deviceId != "全部设备")
        {
            where.Add("DeviceId = @dev");
            paramValues["@dev"] = deviceId;
        }
        if (from.HasValue)
        {
            where.Add("Time >= @from");
            paramValues["@from"] = from.Value.ToString("yyyy-MM-dd 00:00:00");
        }
        if (to.HasValue)
        {
            where.Add("Time <= @to");
            paramValues["@to"] = to.Value.ToString("yyyy-MM-dd 23:59:59.999");
        }

        var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
        total = 0;

        using (var conn = CreateConnection())
        using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM AlarmLogs" + whereSql;
            BindParams(countCmd, paramValues);
            total = Convert.ToInt32(countCmd.ExecuteScalar()!);
        }

        var records = new List<AlarmRecord>();
        using (var conn = CreateConnection())
        using (var query = conn.CreateCommand())
        {
            query.CommandText =
                "SELECT Id, DeviceId, Type, Value, Threshold, Message, Time FROM AlarmLogs" +
                whereSql + " ORDER BY Time DESC LIMIT @limit OFFSET @offset";
            BindParams(query, paramValues);
            query.Parameters.AddWithValue("@limit", pageSize);
            query.Parameters.AddWithValue("@offset", Math.Max(0, (page - 1) * pageSize));
            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new AlarmRecord
                {
                    Id = reader.GetInt64(0),
                    DeviceId = reader.GetString(1),
                    Type = reader.GetString(2),
                    Value = reader.GetDouble(3),
                    Threshold = reader.GetDouble(4),
                    Message = reader.GetString(5),
                    Time = DateTime.Parse(reader.GetString(6))
                });
            }
        }
        return records;
    }

    private static void BindParams(SqliteCommand cmd, Dictionary<string, object> values)
    {
        foreach (var (name, value) in values)
            cmd.Parameters.AddWithValue(name, value);
    }

    public int CountToday()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM AlarmLogs WHERE Time >= @t;";
        cmd.Parameters.AddWithValue("@t", DateTime.Today.ToString("yyyy-MM-dd 00:00:00"));
        return Convert.ToInt32(cmd.ExecuteScalar()!);
    }
}
