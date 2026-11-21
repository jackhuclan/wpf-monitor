namespace IndustrialMonitor.Models;

/// <summary>用户模型，映射 SQLite Users 表。</summary>
public class User
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>角色：管理员 / 操作员 / 观察者。</summary>
    public string Role { get; set; } = "观察者";
    public bool IsActive { get; set; } = true;

    public bool IsAdmin => Role == "管理员";
    /// <summary>是否可以执行操作（观察者只读）。</summary>
    public bool CanOperate => Role != "观察者";
}
