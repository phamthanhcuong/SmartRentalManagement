using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;

namespace RentalManagementSystem.UI.ViewModels;

public partial class VehicleLookupViewModel : BaseViewModel
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehicleLookupViewModel> _logger;

    [ObservableProperty] private ObservableCollection<VehicleLookupItem> _results = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _resultCount;
    [ObservableProperty] private bool _isEmpty;

    public VehicleLookupViewModel(IVehicleService vehicleService, ILogger<VehicleLookupViewModel> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    public override async Task LoadAsync() => await DoSearchAsync();

    // Tìm kiếm tức thời khi gõ
    partial void OnSearchTextChanged(string value) => _ = DoSearchAsync();

    [RelayCommand]
    private async Task SearchAsync() => await DoSearchAsync();

    private async Task DoSearchAsync()
    {
        try
        {
            var items = await _vehicleService.SearchAsync(SearchText);
            Results = new ObservableCollection<VehicleLookupItem>(items);
            ResultCount = Results.Count;
            IsEmpty = ResultCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vehicles");
            ShowError("Lỗi tra cứu xe.");
        }
    }
}
