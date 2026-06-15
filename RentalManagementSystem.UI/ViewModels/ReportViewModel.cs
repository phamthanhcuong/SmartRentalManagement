using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RentalManagementSystem.Application.Interfaces;

namespace RentalManagementSystem.UI.ViewModels;

public partial class ReportViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportViewModel> _logger;

    [ObservableProperty] private int _reportMonth = DateTime.Now.Month;
    [ObservableProperty] private int _reportYear = DateTime.Now.Year;

    public ReportViewModel(IReportService reportService, ILogger<ReportViewModel> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task ExportRevenueAsync()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Excel files|*.xlsx",
            FileName = $"DoanhThu_{ReportMonth:D2}_{ReportYear}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        SetBusy(true, "Đang xuất báo cáo doanh thu...");
        try
        {
            await _reportService.ExportRevenueToExcelAsync(ReportMonth, ReportYear, dlg.FileName);
            ShowSuccess($"Xuất thành công: {dlg.FileName}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error exporting revenue"); ShowError("Lỗi xuất báo cáo."); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task ExportDebtAsync()
    {
        var dlg = new SaveFileDialog { Filter = "Excel files|*.xlsx", FileName = "CongNo.xlsx" };
        if (dlg.ShowDialog() != true) return;
        SetBusy(true, "Đang xuất báo cáo công nợ...");
        try
        {
            await _reportService.ExportDebtToExcelAsync(dlg.FileName);
            ShowSuccess($"Xuất thành công: {dlg.FileName}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error exporting debt"); ShowError("Lỗi xuất báo cáo."); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task ExportUtilityAsync()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Excel files|*.xlsx",
            FileName = $"DienNuoc_{ReportMonth:D2}_{ReportYear}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        SetBusy(true, "Đang xuất báo cáo điện nước...");
        try
        {
            await _reportService.ExportUtilityToExcelAsync(ReportMonth, ReportYear, dlg.FileName);
            ShowSuccess($"Xuất thành công: {dlg.FileName}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error exporting utility"); ShowError("Lỗi xuất báo cáo."); }
        finally { SetBusy(false); }
    }
}
