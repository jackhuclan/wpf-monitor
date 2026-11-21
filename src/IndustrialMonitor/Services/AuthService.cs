using System.Collections;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using IndustrialMonitor.Models;
using Microsoft.Data.Sqlite;

namespace IndustrialMonitor.Services;

/// <summary>认证与权限服务：SHA256 哈希 + RBAC 三级角色。</summary>
public sealed class AuthService : IAuthService
{
    private readonly string _dbPath;

    public AuthService()
    {
        _dbPath = Path.Combine(AppContext.BaseDirectory, "users.db");
        EnsureDatabase();
        EnsureDefaultUsers();
    }

    #region 数据库基础

    private SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private void EnsureDatabase()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                Username TEXT PRIMARY KEY,
                PasswordHash TEXT NOT NULL,
                Role TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );
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

    private void EnsureDefaultUsers()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users;";
        var count = Convert.ToInt64(cmd.ExecuteScalar()!);
        if (count > 0) return;

        InsertUser(conn, "admin", "admin123", "管理员");
        InsertUser(conn, "operator", "123456", "操作员");
        InsertUser(conn, "observer", "123456", "观察者");
    }

    private static void InsertUser(SqliteConnection conn, string username, string password, string role)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Users (Username, PasswordHash, Role, IsActive) VALUES (@u, @p, @r, 1);";
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", Hash(password));
        cmd.Parameters.AddWithValue("@r", role);
        cmd.ExecuteNonQuery();
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    #endregion

    public User? Login(string username, string password)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Username, PasswordHash, Role, IsActive FROM Users WHERE Username = @u;";
        cmd.Parameters.AddWithValue("@u", username);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var user = new User
        {
            Username = reader.GetString(0),
            PasswordHash = reader.GetString(1),
            Role = reader.GetString(2),
            IsActive = reader.GetInt64(3) == 1
        };
        if (!user.IsActive) return null;
        return Hash(password) == user.PasswordHash ? user : null;
    }

    public bool ChangePassword(string username, string oldPassword, string newPassword)
    {
        using var conn = CreateConnection();
        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT PasswordHash FROM Users WHERE Username = @u;";
            check.Parameters.AddWithValue("@u", username);
            var hash = check.ExecuteScalar() as string;
            if (hash == null || Hash(oldPassword) != hash) return false;
        }
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Users SET PasswordHash = @p WHERE Username = @u;";
        cmd.Parameters.AddWithValue("@p", Hash(newPassword));
        cmd.Parameters.AddWithValue("@u", username);
        return cmd.ExecuteNonQuery() > 0;
    }

    public List<User> GetUsers()
    {
        var list = new List<User>();
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Username, PasswordHash, Role, IsActive FROM Users ORDER BY Username;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new User
            {
                Username = reader.GetString(0),
                PasswordHash = reader.GetString(1),
                Role = reader.GetString(2),
                IsActive = reader.GetInt64(3) == 1
            });
        }
        return list;
    }

    public bool AddUser(string username, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;
        using var conn = CreateConnection();
        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @u;";
            check.Parameters.AddWithValue("@u", username);
            if (Convert.ToInt64(check.ExecuteScalar()!) > 0) return false;
        }
        InsertUser(conn, username, password, role);
        return true;
    }

    public bool SetUserActive(string username, bool isActive)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Users SET IsActive = @a WHERE Username = @u;";
        cmd.Parameters.AddWithValue("@a", isActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@u", username);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteUser(string username)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Users WHERE Username = @u;";
        cmd.Parameters.AddWithValue("@u", username);
        return cmd.ExecuteNonQuery() > 0;
    }
}
