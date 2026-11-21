using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

public interface IAuthService
{
    User? Login(string username, string password);
    bool ChangePassword(string username, string oldPassword, string newPassword);
    List<User> GetUsers();
    bool AddUser(string username, string password, string role);
    bool SetUserActive(string username, bool isActive);
    bool DeleteUser(string username);
}
