using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.UI.ViewModels;

public partial class UtilityViewModel : BaseViewModel
{
    private readonly IUtilityService _utilityService;
    private readonly IRoomService _roomService;
    private readonly ILogger<UtilityViewModel> _logger;

    [ObservableProperty] private ObservableCollection<UtilityReading> _readings = new();
    [ObservableProperty] private ObservableCollection<Room> _rooms = new();
    [ObservableProperty] private UtilityReading? _selectedReading;
    [ObservableProperty] private int _filterMonth = DateTime.Now.Month;
    [ObservableProperty] private int _filterYear = DateTime.Now.Year;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;

    // Form
    [ObservableProperty] private int _formRoomId;
    [ObservableProperty] private int _formMonth = DateTime.Now.Month;
    [ObservableProperty] private int _formYear = DateTime.Now.Year;
    [ObservableProperty] private decimal _formElectricOld;
    [ObservableProperty] private decimal _formElectricNew;
    [ObservableProperty] private decimal _formWaterOld;
    [ObservableProperty] private decimal _formWaterNew;
    [ObservableProperty] private decimal _formElectricPrice = 3500;
    [ObservableProperty] private decimal _formWaterPrice = 15000;
    [ObservableProperty] private string _formNote = string.Empty;

    public decimal PreviewElectricAmount => (FormElectricNew - FormElectricOld) * FormElectricPrice;
    public decimal PreviewWaterAmount => (FormWaterNew - FormWaterOld) * FormWaterPrice;
    public decimal PreviewTotal => PreviewElectricAmount + PreviewWaterAmount;

    public UtilityViewModel(IUtilityService utilityService, IRoomService roomService, ILogger<UtilityViewModel> logger)
    {
        _utilityService = utilityService;
        _roomService = roomService;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải dữ liệu điện nước...");
        try
        {
            var readings = await _utilityService.GetByMonthYearAsync(FilterMonth, FilterYear);
            var rooms = await _roomService.GetAllAsync();
            Readings = new ObservableCollection<UtilityReading>(readings);
            Rooms = new ObservableCollection<Room>(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading utility readings");
            ShowError("Lỗi tải dữ liệu điện nước.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task FilterAsync() => await LoadAsync();

    // ===== Nhập nhanh điện/nước hàng loạt =====
    [ObservableProperty] private bool _isBatchDialogOpen;
    [ObservableProperty] private ObservableCollection<UtilityBatchRowVm> _batchRows = new();

    [RelayCommand]
    private async Task OpenBatchEntryAsync()
    {
        SetBusy(true, "Đang chuẩn bị danh sách phòng...");
        try
        {
            var rows = await _utilityService.GetBatchEntryAsync(FilterMonth, FilterYear);
            BatchRows = new ObservableCollection<UtilityBatchRowVm>(rows.Select(r => new UtilityBatchRowVm(r)));
            if (BatchRows.Count == 0) { ShowError("Không có phòng nào đang thuê để nhập chỉ số."); return; }
            IsBatchDialogOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening batch utility entry");
            ShowError("Lỗi tải danh sách nhập nhanh.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task SaveBatchAsync()
    {
        // Kiểm tra chỉ số hợp lệ trước khi lưu
        var invalid = BatchRows.FirstOrDefault(r => r.ElectricNew < r.ElectricOld || r.WaterNew < r.WaterOld);
        if (invalid != null) { ShowError($"Phòng {invalid.RoomCode}: chỉ số mới không được nhỏ hơn chỉ số cũ."); return; }

        SetBusy(true, "Đang lưu...");
        try
        {
            var readings = BatchRows.Select(r => new UtilityReading
            {
                RoomId = r.RoomId,
                ElectricOld = r.ElectricOld,
                ElectricNew = r.ElectricNew,
                WaterOld = r.WaterOld,
                WaterNew = r.WaterNew,
                ElectricPrice = r.ElectricPrice,
                WaterPrice = r.WaterPrice
            });
            var count = await _utilityService.SaveBatchAsync(FilterMonth, FilterYear, readings);
            IsBatchDialogOpen = false;
            ShowSuccess($"Đã lưu chỉ số cho {count} phòng.");
            await LoadAsync();
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void CloseBatchDialog() => IsBatchDialogOpen = false;

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditing = false;
        FormRoomId = 0; FormMonth = FilterMonth; FormYear = FilterYear;
        FormElectricOld = FormElectricNew = FormWaterOld = FormWaterNew = 0;
        FormElectricPrice = 3500; FormWaterPrice = 15000; FormNote = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(UtilityReading reading)
    {
        IsEditing = true;
        SelectedReading = reading;
        FormRoomId = reading.RoomId;
        FormMonth = reading.Month;
        FormYear = reading.Year;
        FormElectricOld = reading.ElectricOld;
        FormElectricNew = reading.ElectricNew;
        FormWaterOld = reading.WaterOld;
        FormWaterNew = reading.WaterNew;
        FormElectricPrice = reading.ElectricPrice;
        FormWaterPrice = reading.WaterPrice;
        FormNote = reading.Note ?? string.Empty;
        IsDialogOpen = true;
    }

    partial void OnFormElectricNewChanged(decimal value) => OnPropertyChanged(nameof(PreviewElectricAmount));
    partial void OnFormWaterNewChanged(decimal value) => OnPropertyChanged(nameof(PreviewWaterAmount));
    partial void OnFormElectricOldChanged(decimal value) => OnPropertyChanged(nameof(PreviewElectricAmount));
    partial void OnFormWaterOldChanged(decimal value) => OnPropertyChanged(nameof(PreviewWaterAmount));
    partial void OnFormElectricPriceChanged(decimal value) => OnPropertyChanged(nameof(PreviewElectricAmount));
    partial void OnFormWaterPriceChanged(decimal value) => OnPropertyChanged(nameof(PreviewWaterAmount));

    // Khi chọn phòng (lúc thêm mới): tự kế thừa chỉ số đầu kỳ + đơn giá theo khu
    partial void OnFormRoomIdChanged(int value)
    {
        if (IsEditing || value == 0) return;
        _ = AutoFillFromPreviousAsync(value);
    }

    private async Task AutoFillFromPreviousAsync(int roomId)
    {
        try
        {
            // Đơn giá mặc định theo khu của phòng
            var room = Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room?.RentalArea != null)
            {
                FormElectricPrice = room.RentalArea.ElectricPrice;
                FormWaterPrice = room.RentalArea.WaterPrice;
            }

            // Chỉ số đầu kỳ = chỉ số cuối kỳ trước
            var prev = await _utilityService.GetPreviousReadingAsync(roomId, FormMonth, FormYear);
            if (prev != null)
            {
                FormElectricOld = prev.ElectricNew;
                FormWaterOld = prev.WaterNew;
                if (FormElectricNew < FormElectricOld) FormElectricNew = FormElectricOld;
                if (FormWaterNew < FormWaterOld) FormWaterNew = FormWaterOld;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-filling utility from previous reading");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (FormRoomId == 0) { ShowError("Vui lòng chọn phòng."); return; }
        if (FormElectricNew < FormElectricOld) { ShowError("Chỉ số điện mới phải >= chỉ số cũ."); return; }
        if (FormWaterNew < FormWaterOld) { ShowError("Chỉ số nước mới phải >= chỉ số cũ."); return; }

        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedReading != null)
            {
                SelectedReading.ElectricOld = FormElectricOld;
                SelectedReading.ElectricNew = FormElectricNew;
                SelectedReading.WaterOld = FormWaterOld;
                SelectedReading.WaterNew = FormWaterNew;
                SelectedReading.ElectricPrice = FormElectricPrice;
                SelectedReading.WaterPrice = FormWaterPrice;
                SelectedReading.Note = FormNote;
                result = await _utilityService.UpdateAsync(SelectedReading);
            }
            else
            {
                var reading = new UtilityReading
                {
                    RoomId = FormRoomId,
                    Month = FormMonth,
                    Year = FormYear,
                    ElectricOld = FormElectricOld,
                    ElectricNew = FormElectricNew,
                    WaterOld = FormWaterOld,
                    WaterNew = FormWaterNew,
                    ElectricPrice = FormElectricPrice,
                    WaterPrice = FormWaterPrice,
                    Note = FormNote
                };
                result = await _utilityService.CreateAsync(reading);
            }

            if (result)
            {
                ShowSuccess("Lưu thành công!");
                IsDialogOpen = false;
                await LoadAsync();
            }
            else ShowError("Có lỗi xảy ra.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task DeleteAsync(UtilityReading reading)
    {
        if (!ConfirmDelete($"chỉ số điện nước phòng {reading.Room?.RoomCode} kỳ {reading.Month:D2}/{reading.Year}")) return;
        if (await _utilityService.DeleteAsync(reading.Id)) await LoadAsync();
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;
}

/// <summary>Dòng nhập nhanh điện/nước (có tính tiền realtime khi gõ chỉ số mới).</summary>
public partial class UtilityBatchRowVm : ObservableObject
{
    public int RoomId { get; }
    public string RoomCode { get; }
    public string AreaName { get; }
    public decimal ElectricPrice { get; }
    public decimal WaterPrice { get; }

    [ObservableProperty] private decimal _electricOld;
    [ObservableProperty] private decimal _electricNew;
    [ObservableProperty] private decimal _waterOld;
    [ObservableProperty] private decimal _waterNew;

    public UtilityBatchRowVm(UtilityBatchRow r)
    {
        RoomId = r.RoomId;
        RoomCode = r.RoomCode;
        AreaName = r.AreaName;
        ElectricPrice = r.ElectricPrice;
        WaterPrice = r.WaterPrice;
        _electricOld = r.ElectricOld;
        _waterOld = r.WaterOld;
        _electricNew = r.ElectricNew;
        _waterNew = r.WaterNew;
    }

    public decimal ElectricUsage => ElectricNew - ElectricOld;
    public decimal WaterUsage => WaterNew - WaterOld;
    public decimal RowTotal => ElectricUsage * ElectricPrice + WaterUsage * WaterPrice;

    partial void OnElectricNewChanged(decimal value)
    {
        OnPropertyChanged(nameof(ElectricUsage));
        OnPropertyChanged(nameof(RowTotal));
    }
    partial void OnWaterNewChanged(decimal value)
    {
        OnPropertyChanged(nameof(WaterUsage));
        OnPropertyChanged(nameof(RowTotal));
    }
}
