using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.UI.ViewModels;

public partial class RentalAreaViewModel : BaseViewModel
{
    private readonly IRentalAreaService _service;
    private readonly ILogger<RentalAreaViewModel> _logger;

    [ObservableProperty] private ObservableCollection<RentalArea> _areas = new();
    [ObservableProperty] private RentalArea? _selectedArea;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private string _formAreaCode = string.Empty;
    [ObservableProperty] private string _formAreaName = string.Empty;
    [ObservableProperty] private string _formAddress = string.Empty;
    [ObservableProperty] private string _formOwnerName = string.Empty;
    [ObservableProperty] private string _formOwnerPhone = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;

    public RentalAreaViewModel(IRentalAreaService service, ILogger<RentalAreaViewModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true);
        try
        {
            Areas = new ObservableCollection<RentalArea>(await _service.GetAllAsync());
        }
        catch (Exception ex) { _logger.LogError(ex, "Error loading areas"); ShowError("Lỗi tải dữ liệu."); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditing = false;
        FormAreaCode = FormAreaName = FormAddress = FormOwnerName = FormOwnerPhone = FormDescription = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(RentalArea area)
    {
        IsEditing = true;
        SelectedArea = area;
        FormAreaCode = area.AreaCode;
        FormAreaName = area.AreaName;
        FormAddress = area.Address ?? string.Empty;
        FormOwnerName = area.OwnerName ?? string.Empty;
        FormOwnerPhone = area.OwnerPhone ?? string.Empty;
        FormDescription = area.Description ?? string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormAreaCode) || string.IsNullOrWhiteSpace(FormAreaName))
        {
            ShowError("Mã và tên khu trọ không được để trống."); return;
        }

        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedArea != null)
            {
                SelectedArea.AreaCode = FormAreaCode;
                SelectedArea.AreaName = FormAreaName;
                SelectedArea.Address = FormAddress;
                SelectedArea.OwnerName = FormOwnerName;
                SelectedArea.OwnerPhone = FormOwnerPhone;
                SelectedArea.Description = FormDescription;
                result = await _service.UpdateAsync(SelectedArea);
            }
            else
            {
                result = await _service.CreateAsync(new RentalArea
                {
                    AreaCode = FormAreaCode, AreaName = FormAreaName,
                    Address = FormAddress, OwnerName = FormOwnerName,
                    OwnerPhone = FormOwnerPhone, Description = FormDescription
                });
            }

            if (result) { ShowSuccess("Lưu thành công!"); IsDialogOpen = false; await LoadAsync(); }
            else ShowError("Có lỗi xảy ra.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task DeleteAsync(RentalArea area)
    {
        if (!ConfirmDelete($"khu trọ \"{area.AreaName}\"")) return;
        if (await _service.DeleteAsync(area.Id)) await LoadAsync();
    }

    [RelayCommand] private void CloseDialog() => IsDialogOpen = false;
}
