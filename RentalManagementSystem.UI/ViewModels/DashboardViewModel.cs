using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using SkiaSharp;

namespace RentalManagementSystem.UI.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty] private int _totalRooms;
    [ObservableProperty] private int _availableRooms;
    [ObservableProperty] private int _occupiedRooms;
    [ObservableProperty] private int _totalTenants;
    [ObservableProperty] private decimal _totalUnpaid;
    [ObservableProperty] private decimal _monthlyRevenue;
    [ObservableProperty] private int _expiringContracts;
    [ObservableProperty] private ISeries[] _revenueSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _revenueXAxes = Array.Empty<Axis>();
    [ObservableProperty] private ISeries[] _occupancySeries = Array.Empty<ISeries>();
    [ObservableProperty] private string _currentDateDisplay = DateTime.Now.ToString("dddd, dd MMMM yyyy");

    // Cảnh báo hành động
    [ObservableProperty] private int _roomsMissingReading;
    [ObservableProperty] private int _roomsNoInvoice;
    [ObservableProperty] private int _overdueInvoices;
    [ObservableProperty] private decimal _overdueAmount;
    [ObservableProperty] private int _alertExpiringContracts;
    [ObservableProperty] private bool _hasAlerts;

    public DashboardViewModel(IDashboardService dashboardService, ILogger<DashboardViewModel> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải dashboard...");
        try
        {
            var stats = await _dashboardService.GetStatsAsync();
            TotalRooms = stats.TotalRooms;
            AvailableRooms = stats.AvailableRooms;
            OccupiedRooms = stats.OccupiedRooms;
            TotalTenants = stats.TotalTenants;
            TotalUnpaid = stats.TotalUnpaid;
            MonthlyRevenue = stats.MonthlyRevenue;
            ExpiringContracts = stats.ExpiringContracts;

            var alerts = await _dashboardService.GetAlertsAsync();
            RoomsMissingReading = alerts.RoomsMissingReading;
            RoomsNoInvoice = alerts.RoomsNoInvoice;
            OverdueInvoices = alerts.OverdueInvoices;
            OverdueAmount = alerts.OverdueAmount;
            AlertExpiringContracts = alerts.ExpiringContracts;
            HasAlerts = RoomsMissingReading > 0 || RoomsNoInvoice > 0 || OverdueInvoices > 0 || AlertExpiringContracts > 0;

            await LoadChartsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            ShowError("Lỗi tải dashboard.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadChartsAsync()
    {
        var monthlyData = (await _dashboardService.GetMonthlyRevenueAsync(DateTime.Now.Year)).ToList();

        var revenueValues = monthlyData.Select(m => (double)m.Revenue).ToArray();
        var expenseValues = monthlyData.Select(m => (double)m.Expense).ToArray();

        RevenueSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Doanh thu",
                Values = revenueValues,
                Fill = new SolidColorPaint(SKColor.Parse("#7A1E3A")),
                Rx = 4, Ry = 4
            },
            new LineSeries<double>
            {
                Name = "Chi phí",
                Values = expenseValues,
                Stroke = new SolidColorPaint(SKColor.Parse("#C8A14B"), 3),
                GeometryFill = new SolidColorPaint(SKColor.Parse("#C8A14B")),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#C8A14B"), 2),
                GeometrySize = 8
            }
        };

        RevenueXAxes = new[]
        {
            new Axis
            {
                Labels = new[] { "T1","T2","T3","T4","T5","T6","T7","T8","T9","T10","T11","T12" },
                TextSize = 12
            }
        };

        var occupancyData = (await _dashboardService.GetOccupancyRateAsync()).ToList();
        OccupancySeries = new ISeries[]
        {
            new PieSeries<double>
            {
                Name = "Đang thuê",
                Values = new[] { (double)occupancyData.Sum(o => o.Occupied) },
                Fill = new SolidColorPaint(SKColor.Parse("#7A1E3A"))
            },
            new PieSeries<double>
            {
                Name = "Trống",
                Values = new[] { (double)occupancyData.Sum(o => o.Total - o.Occupied) },
                Fill = new SolidColorPaint(SKColor.Parse("#E0E0E0"))
            }
        };
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void Navigate(string menu) => MainViewModel.NavigateRequested?.Invoke(menu);
}
