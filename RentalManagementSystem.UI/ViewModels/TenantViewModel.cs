using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.UI.ViewModels;

public partial class TenantViewModel : BaseViewModel
{
    private readonly ITenantService _tenantService;
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<TenantViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Tenant> _tenants = new();
    [ObservableProperty] private Tenant? _selectedTenant;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;

    // Form fields
    [ObservableProperty] private string _formFullName = string.Empty;
    [ObservableProperty] private string _formPhone = string.Empty;
    [ObservableProperty] private string _formEmail = string.Empty;
    [ObservableProperty] private string _formCCCD = string.Empty;
    [ObservableProperty] private DateTime? _formDateOfBirth;
    [ObservableProperty] private string _formAddress = string.Empty;
    [ObservableProperty] private string _formOccupation = string.Empty;
    [ObservableProperty] private string _formAvatarPath = string.Empty;
    [ObservableProperty] private string _formNote = string.Empty;

    // Quản lý xe
    [ObservableProperty] private bool _isVehicleDialogOpen;
    [ObservableProperty] private Tenant? _vehicleTenant;
    [ObservableProperty] private ObservableCollection<Vehicle> _vehicles = new();
    [ObservableProperty] private string _vehLicensePlate = string.Empty;
    [ObservableProperty] private RentalManagementSystem.Domain.Enums.VehicleType _vehType = RentalManagementSystem.Domain.Enums.VehicleType.Motorbike;
    [ObservableProperty] private string _vehBrand = string.Empty;
    [ObservableProperty] private string _vehColor = string.Empty;

    public Array VehicleTypes => Enum.GetValues(typeof(RentalManagementSystem.Domain.Enums.VehicleType));

    public TenantViewModel(ITenantService tenantService, IVehicleService vehicleService, ILogger<TenantViewModel> logger)
    {
        _tenantService = tenantService;
        _vehicleService = vehicleService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task OpenVehicleDialogAsync(Tenant tenant)
    {
        VehicleTenant = tenant;
        VehLicensePlate = string.Empty; VehBrand = string.Empty; VehColor = string.Empty;
        VehType = RentalManagementSystem.Domain.Enums.VehicleType.Motorbike;
        try
        {
            var list = await _vehicleService.GetByTenantAsync(tenant.Id);
            Vehicles = new ObservableCollection<Vehicle>(list);
            IsVehicleDialogOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicles");
            ShowError("Lỗi tải danh sách xe.");
        }
    }

    [RelayCommand]
    private async Task AddVehicleAsync()
    {
        if (VehicleTenant == null) return;
        if (string.IsNullOrWhiteSpace(VehLicensePlate)) { ShowError("Vui lòng nhập biển số xe."); return; }
        var v = new Vehicle
        {
            TenantId = VehicleTenant.Id,
            LicensePlate = VehLicensePlate,
            VehicleType = VehType,
            Brand = VehBrand,
            Color = VehColor
        };
        if (await _vehicleService.AddAsync(v))
        {
            Vehicles = new ObservableCollection<Vehicle>(await _vehicleService.GetByTenantAsync(VehicleTenant.Id));
            VehLicensePlate = string.Empty; VehBrand = string.Empty; VehColor = string.Empty;
        }
        else ShowError("Không thể thêm: biển số đã tồn tại hoặc dữ liệu không hợp lệ.");
    }

    [RelayCommand]
    private async Task RemoveVehicleAsync(Vehicle vehicle)
    {
        if (await _vehicleService.RemoveAsync(vehicle.Id) && VehicleTenant != null)
            Vehicles = new ObservableCollection<Vehicle>(await _vehicleService.GetByTenantAsync(VehicleTenant.Id));
    }

    [RelayCommand]
    private void CloseVehicleDialog() => IsVehicleDialogOpen = false;

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải danh sách khách thuê...");
        try
        {
            var tenants = await _tenantService.GetAllAsync();
            Tenants = new ObservableCollection<Tenant>(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenants");
            ShowError("Lỗi tải danh sách khách thuê.");
        }
        finally { SetBusy(false); }
    }

    partial void OnSearchTextChanged(string value) => _ = SearchAsync();

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) { await LoadAsync(); return; }
        var results = await _tenantService.SearchAsync(SearchText);
        Tenants = new ObservableCollection<Tenant>(results);
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditing = false;
        ClearForm();
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Tenant tenant)
    {
        IsEditing = true;
        SelectedTenant = tenant;
        FormFullName = tenant.FullName;
        FormPhone = tenant.Phone;
        FormEmail = tenant.Email ?? string.Empty;
        FormCCCD = tenant.CCCD ?? string.Empty;
        FormDateOfBirth = tenant.DateOfBirth;
        FormAddress = tenant.Address ?? string.Empty;
        FormOccupation = tenant.Occupation ?? string.Empty;
        FormAvatarPath = tenant.AvatarPath ?? string.Empty;
        FormNote = tenant.Note ?? string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void UploadAvatar()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*",
            Title = "Chọn ảnh đại diện"
        };
        if (dlg.ShowDialog() == true)
            FormAvatarPath = dlg.FileName;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormFullName))
        {
            ShowError("Họ tên không được để trống.");
            return;
        }
        if (string.IsNullOrWhiteSpace(FormPhone))
        {
            ShowError("Số điện thoại không được để trống.");
            return;
        }

        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedTenant != null)
            {
                SelectedTenant.FullName = FormFullName;
                SelectedTenant.Phone = FormPhone;
                SelectedTenant.Email = FormEmail;
                SelectedTenant.CCCD = FormCCCD;
                SelectedTenant.DateOfBirth = FormDateOfBirth;
                SelectedTenant.Address = FormAddress;
                SelectedTenant.Occupation = FormOccupation;
                SelectedTenant.AvatarPath = FormAvatarPath;
                SelectedTenant.Note = FormNote;
                result = await _tenantService.UpdateAsync(SelectedTenant);
            }
            else
            {
                var tenant = new Tenant
                {
                    FullName = FormFullName,
                    Phone = FormPhone,
                    Email = FormEmail,
                    CCCD = FormCCCD,
                    DateOfBirth = FormDateOfBirth,
                    Address = FormAddress,
                    Occupation = FormOccupation,
                    AvatarPath = FormAvatarPath,
                    Note = FormNote
                };
                result = await _tenantService.CreateAsync(tenant);
            }

            if (result)
            {
                ShowSuccess(IsEditing ? "Cập nhật thành công!" : "Thêm khách thuê thành công!");
                IsDialogOpen = false;
                await LoadAsync();
            }
            else ShowError("Có lỗi xảy ra.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tenant");
            ShowError("Lỗi khi lưu khách thuê.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task DeleteAsync(Tenant tenant)
    {
        if (!ConfirmDelete($"khách thuê \"{tenant.FullName}\"")) return;

        if (await _tenantService.DeleteAsync(tenant.Id))
        {
            ShowSuccess("Xóa thành công!");
            await LoadAsync();
        }
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;

    private void ClearForm()
    {
        FormFullName = FormPhone = FormEmail = FormCCCD =
        FormAddress = FormOccupation = FormAvatarPath = FormNote = string.Empty;
        FormDateOfBirth = null;
    }
}
