using IndustrialMonitor.Models;

namespace IndustrialMonitor.ViewModels;

/// <summary>页面切回时刷新数据的约定接口。</summary>
public interface IRefreshable
{
    void Refresh();
}

/// <summary>需要知道当前登录用户的 ViewModel 约定（用于按钮级权限）。</summary>
public interface IUserAware
{
    void SetUserContext(User user);
}
