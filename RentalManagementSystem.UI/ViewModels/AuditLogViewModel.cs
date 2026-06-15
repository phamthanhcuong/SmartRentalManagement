using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.UI.ViewModels;

public partial class AuditLogViewModel : BaseViewModel
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditLogViewModel> _logger;

    [ObservableProperty] private ObservableCollection<AuditLog> _logs = new();

    public AuditLogViewModel(IAuditService auditService, ILogger<AuditLogViewModel> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải nhật ký...");
        try
        {
            var items = await _auditService.GetRecentAsync();
            Logs = new ObservableCollection<AuditLog>(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit logs");
            ShowError("Lỗi tải nhật ký.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
