namespace IndustrialMonitor.Models;

/// <summary>侧边栏导航菜单项模型。</summary>
public class NavMenuItem
{
    public string Title { get; set; } = string.Empty;
    public string Glyph { get; set; } = string.Empty;
    public Type TargetViewModelType { get; set; } = default!;
    /// <summary>允许访问的角色列表（管理员/操作员/观察者）。</summary>
    public string[] AllowedRoles { get; set; } = Array.Empty<string>();
    /// <summary>当前是否选中（供侧边栏高亮）。</summary>
    public bool IsActive { get; set; }
}
