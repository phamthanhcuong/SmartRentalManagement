using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.UI.ViewModels;

public partial class ServiceViewModel : BaseViewModel
{
    private readonly IServiceService _service;
    private readonly ILogger<ServiceViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Domain.Entities.Service> _services = new();
    [ObservableProperty] private Domain.Entities.Service? _selectedService;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private ServiceType _formType = ServiceType.Other;
    [ObservableProperty] private decimal _formPrice;
    [ObservableProperty] private string _formUnit = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;

    public ServiceViewModel(IServiceService service, ILogger<ServiceViewModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true);
        try { Services = new ObservableCollection<Domain.Entities.Service>(await _service.GetAllAsync()); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading services"); ShowError("Lỗi tải dữ liệu."); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditing = false;
        FormName = FormUnit = FormDescription = string.Empty;
        FormType = ServiceType.Other; FormPrice = 0;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Domain.Entities.Service svc)
    {
        IsEditing = true; SelectedService = svc;
        FormName = svc.ServiceName; FormType = svc.ServiceType;
        FormPrice = svc.UnitPrice; FormUnit = svc.Unit ?? string.Empty;
        FormDescription = svc.Description ?? string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { ShowError("Tên dịch vụ không được để trống."); return; }
        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedService != null)
            {
                SelectedService.ServiceName = FormName; SelectedService.ServiceType = FormType;
                SelectedService.UnitPrice = FormPrice; SelectedService.Unit = FormUnit;
                SelectedService.Description = FormDescription;
                result = await _service.UpdateAsync(SelectedService);
            }
            else
                result = await _service.CreateAsync(new Domain.Entities.Service
                { ServiceName = FormName, ServiceType = FormType, UnitPrice = FormPrice, Unit = FormUnit, Description = FormDescription });

            if (result) { ShowSuccess("Lưu thành công!"); IsDialogOpen = false; await LoadAsync(); }
            else ShowError("Có lỗi xảy ra.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task DeleteAsync(Domain.Entities.Service svc)
    {
        if (!ConfirmDelete($"dịch vụ \"{svc.ServiceName}\"")) return;
        if (await _service.DeleteAsync(svc.Id)) await LoadAsync();
    }

    [RelayCommand] private void CloseDialog() => IsDialogOpen = false;
}
