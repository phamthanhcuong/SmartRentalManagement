using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.UI.ViewModels;

public partial class InvoiceViewModel : BaseViewModel
{
    private readonly IInvoiceService _invoiceService;
    private readonly IRoomService _roomService;
    private readonly ITenantService _tenantService;
    private readonly IReportService _reportService;
    private readonly Services.InvoicePrintService _printService;
    private readonly ILogger<InvoiceViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Invoice> _invoices = new();
    [ObservableProperty] private ObservableCollection<Room> _rooms = new();
    [ObservableProperty] private ObservableCollection<Tenant> _tenants = new();
    [ObservableProperty] private Invoice? _selectedInvoice;
    [ObservableProperty] private int _filterMonth = DateTime.Now.Month;
    [ObservableProperty] private int _filterYear = DateTime.Now.Year;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isPayDialogOpen;
    [ObservableProperty] private decimal _payAmount;

    // Form
    [ObservableProperty] private string _formInvoiceNo = string.Empty;
    [ObservableProperty] private int _formRoomId;
    [ObservableProperty] private int _formTenantId;
    [ObservableProperty] private int _formMonth = DateTime.Now.Month;
    [ObservableProperty] private int _formYear = DateTime.Now.Year;
    [ObservableProperty] private decimal _formRentAmount;
    [ObservableProperty] private decimal _formElectricAmount;
    [ObservableProperty] private decimal _formWaterAmount;
    [ObservableProperty] private decimal _formServiceAmount;
    [ObservableProperty] private decimal _formDiscountAmount;
    [ObservableProperty] private string _formNote = string.Empty;
    public decimal FormTotalAmount => FormRentAmount + FormElectricAmount + FormWaterAmount + FormServiceAmount - FormDiscountAmount;

    public InvoiceViewModel(IInvoiceService invoiceService, IRoomService roomService,
        ITenantService tenantService, IReportService reportService,
        Services.InvoicePrintService printService, ILogger<InvoiceViewModel> logger)
    {
        _invoiceService = invoiceService;
        _roomService = roomService;
        _tenantService = tenantService;
        _reportService = reportService;
        _printService = printService;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải hóa đơn...");
        try
        {
            var invoices = await _invoiceService.GetByMonthYearAsync(FilterMonth, FilterYear);
            var rooms = await _roomService.GetAllAsync();
            var tenants = await _tenantService.GetAllAsync();
            Invoices = new ObservableCollection<Invoice>(invoices);
            Rooms = new ObservableCollection<Room>(rooms);
            Tenants = new ObservableCollection<Tenant>(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading invoices");
            ShowError("Lỗi tải hóa đơn.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task FilterAsync() => await LoadAsync();

    [RelayCommand]
    private async Task GenerateMonthlyAsync()
    {
        var r = System.Windows.MessageBox.Show(
            $"Tạo hóa đơn tháng {FilterMonth:D2}/{FilterYear} cho tất cả phòng?",
            "Xác nhận", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (r != System.Windows.MessageBoxResult.Yes) return;

        SetBusy(true, "Đang tạo hóa đơn...");
        try
        {
            await _invoiceService.GenerateMonthlyInvoicesAsync(FilterMonth, FilterYear);
            ShowSuccess("Tạo hóa đơn thành công!");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly invoices");
            ShowError("Lỗi tạo hóa đơn.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void OpenPayDialog(Invoice invoice)
    {
        SelectedInvoice = invoice;
        PayAmount = invoice.TotalAmount - invoice.PaidAmount;
        IsPayDialogOpen = true;
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (SelectedInvoice == null || PayAmount <= 0) { ShowError("Số tiền không hợp lệ."); return; }

        SetBusy(true, "Đang xử lý thanh toán...");
        try
        {
            if (await _invoiceService.PayAsync(SelectedInvoice.Id, PayAmount))
            {
                ShowSuccess("Thanh toán thành công!");
                IsPayDialogOpen = false;
                await LoadAsync();
            }
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task PrintAsync(Invoice invoice)
    {
        SetBusy(true, "Đang in hóa đơn...");
        try
        {
            // Tự động: in ra máy in vật lý nếu có, không thì in ra PDF (Microsoft Print to PDF)
            var result = await _printService.PrintInvoiceAsync(invoice.Id);
            if (result.Success)
                ShowSuccess(result.UsedPhysicalPrinter
                    ? $"Đã gửi hóa đơn tới máy in: {result.PrinterName}"
                    : $"Đã in ra PDF qua: {result.PrinterName}");
            else
                ShowError(result.Message ?? "Không thể in hóa đơn.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing invoice");
            ShowError("Lỗi in hóa đơn.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task DeleteAsync(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Paid) { ShowError("Không thể xóa hóa đơn đã thanh toán."); return; }
        if (!ConfirmDelete($"hóa đơn \"{invoice.InvoiceNo}\" (phòng {invoice.Room?.RoomCode})")) return;
        if (await _invoiceService.DeleteAsync(invoice.Id)) await LoadAsync();
    }

    [RelayCommand]
    private void CloseDialog() { IsDialogOpen = false; IsPayDialogOpen = false; }

    partial void OnFormRentAmountChanged(decimal v) => OnPropertyChanged(nameof(FormTotalAmount));
    partial void OnFormElectricAmountChanged(decimal v) => OnPropertyChanged(nameof(FormTotalAmount));
    partial void OnFormWaterAmountChanged(decimal v) => OnPropertyChanged(nameof(FormTotalAmount));
    partial void OnFormServiceAmountChanged(decimal v) => OnPropertyChanged(nameof(FormTotalAmount));
    partial void OnFormDiscountAmountChanged(decimal v) => OnPropertyChanged(nameof(FormTotalAmount));
}
