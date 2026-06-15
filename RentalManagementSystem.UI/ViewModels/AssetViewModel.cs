using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.UI.ViewModels;

public partial class AssetViewModel : BaseViewModel
{
    private readonly IAssetService _service;
    private readonly ILogger<AssetViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Asset> _assets = new();
    [ObservableProperty] private Asset? _selectedAsset;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private string _formCode = string.Empty;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formCategory = string.Empty;
    [ObservableProperty] private decimal _formPrice;
    [ObservableProperty] private DateTime? _formPurchaseDate;
    [ObservableProperty] private string _formCondition = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;

    public AssetViewModel(IAssetService service, ILogger<AssetViewModel> logger)
    {
        _service = service; _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true);
        try { Assets = new ObservableCollection<Asset>(await _service.GetAllAsync()); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading assets"); ShowError("Lỗi tải dữ liệu."); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditing = false;
        FormCode = FormName = FormCategory = FormCondition = FormDescription = string.Empty;
        FormPrice = 0; FormPurchaseDate = null;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Asset asset)
    {
        IsEditing = true; SelectedAsset = asset;
        FormCode = asset.AssetCode; FormName = asset.AssetName;
        FormCategory = asset.Category ?? string.Empty;
        FormPrice = asset.PurchasePrice; FormPurchaseDate = asset.PurchaseDate;
        FormCondition = asset.Condition ?? string.Empty;
        FormDescription = asset.Description ?? string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { ShowError("Tên tài sản không được để trống."); return; }
        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedAsset != null)
            {
                SelectedAsset.AssetCode = FormCode; SelectedAsset.AssetName = FormName;
                SelectedAsset.Category = FormCategory; SelectedAsset.PurchasePrice = FormPrice;
                SelectedAsset.PurchaseDate = FormPurchaseDate; SelectedAsset.Condition = FormCondition;
                SelectedAsset.Description = FormDescription;
                result = await _service.UpdateAsync(SelectedAsset);
            }
            else
                result = await _service.CreateAsync(new Asset
                {
                    AssetCode = FormCode, AssetName = FormName, Category = FormCategory,
                    PurchasePrice = FormPrice, PurchaseDate = FormPurchaseDate,
                    Condition = FormCondition, Description = FormDescription
                });

            if (result) { ShowSuccess("Lưu thành công!"); IsDialogOpen = false; await LoadAsync(); }
            else ShowError("Có lỗi xảy ra.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task DeleteAsync(Asset asset)
    {
        if (!ConfirmDelete($"tài sản \"{asset.AssetName}\"")) return;
        if (await _service.DeleteAsync(asset.Id)) await LoadAsync();
    }

    [RelayCommand] private void CloseDialog() => IsDialogOpen = false;
}
