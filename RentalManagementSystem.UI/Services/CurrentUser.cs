using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.UI.ViewModels;

namespace RentalManagementSystem.UI.Services;

/// <summary>Lấy người dùng đang đăng nhập từ phiên hiện tại để gắn vào nhật ký.</summary>
public class CurrentUser : ICurrentUser
{
    public string UserName => LoginViewModel.CurrentUser?.Username ?? "(chưa đăng nhập)";
}
