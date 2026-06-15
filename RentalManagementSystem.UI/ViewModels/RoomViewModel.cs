using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.UI.ViewModels;

public partial class RoomViewModel : BaseViewModel
{
    private readonly IRoomService _roomService;
    private readonly IRentalAreaService _areaService;
    private readonly ILogger<RoomViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Room> _rooms = new();
    [ObservableProperty] private ObservableCollection<RentalArea> _areas = new();
    [ObservableProperty] private Room? _selectedRoom;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;

    // Form fields
    [ObservableProperty] private string _formRoomCode = string.Empty;
    [ObservableProperty] private string _formRoomName = string.Empty;
    [ObservableProperty] private int _formAreaId;
    [ObservableProperty] private decimal _formArea;
    [ObservableProperty] private decimal _formPrice;
    [ObservableProperty] private decimal _formDeposit;
    [ObservableProperty] private int _formMaxOccupants = 2;
    [ObservableProperty] private RoomStatus _formStatus = RoomStatus.Available;
    [ObservableProperty] private string _formDescription = string.Empty;

    public RoomViewModel(IRoomService roomService, IRentalAreaService areaService, ILogger<RoomViewModel> logger)
    {
        _roomService = roomService;
        _areaService = areaService;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải danh sách phòng...");
        try
        {
            var rooms = await _roomService.GetAllAsync();
            var areas = await _areaService.GetAllAsync();
            Rooms = new ObservableCollection<Room>(rooms);
            Areas = new ObservableCollection<RentalArea>(areas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading rooms");
            ShowError("Lỗi tải danh sách phòng.");
        }
        finally { SetBusy(false); }
    }

    partial void OnSearchTextChanged(string value) => _ = SearchAsync();

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadAsync();
            return;
        }
        var results = await _roomService.SearchAsync(SearchText);
        Rooms = new ObservableCollection<Room>(results);
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditing = false;
        ClearForm();
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Room room)
    {
        IsEditing = true;
        SelectedRoom = room;
        FormRoomCode = room.RoomCode;
        FormRoomName = room.RoomName;
        FormAreaId = room.RentalAreaId;
        FormArea = room.Area;
        FormPrice = room.Price;
        FormDeposit = room.Deposit;
        FormMaxOccupants = room.MaxOccupants;
        FormStatus = room.Status;
        FormDescription = room.Description ?? string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormRoomCode))
        {
            ShowError("Mã phòng không được để trống.");
            return;
        }

        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedRoom != null)
            {
                SelectedRoom.RoomCode = FormRoomCode;
                SelectedRoom.RoomName = FormRoomName;
                SelectedRoom.RentalAreaId = FormAreaId;
                SelectedRoom.Area = FormArea;
                SelectedRoom.Price = FormPrice;
                SelectedRoom.Deposit = FormDeposit;
                SelectedRoom.MaxOccupants = FormMaxOccupants;
                SelectedRoom.Status = FormStatus;
                SelectedRoom.Description = FormDescription;
                result = await _roomService.UpdateAsync(SelectedRoom);
            }
            else
            {
                var room = new Room
                {
                    RoomCode = FormRoomCode,
                    RoomName = FormRoomName,
                    RentalAreaId = FormAreaId,
                    Area = FormArea,
                    Price = FormPrice,
                    Deposit = FormDeposit,
                    MaxOccupants = FormMaxOccupants,
                    Status = FormStatus,
                    Description = FormDescription
                };
                result = await _roomService.CreateAsync(room);
            }

            if (result)
            {
                ShowSuccess(IsEditing ? "Cập nhật phòng thành công!" : "Thêm phòng thành công!");
                IsDialogOpen = false;
                await LoadAsync();
            }
            else
                ShowError("Có lỗi xảy ra. Vui lòng thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving room");
            ShowError("Lỗi khi lưu phòng.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task DeleteAsync(Room room)
    {
        if (room.Status == RoomStatus.Occupied)
        {
            ShowError("Không thể xóa phòng đang có khách thuê.");
            return;
        }

        if (!ConfirmDelete($"phòng \"{room.RoomCode}\"" + (string.IsNullOrWhiteSpace(room.RoomName) ? "" : $" ({room.RoomName})"))) return;

        SetBusy(true);
        try
        {
            if (await _roomService.DeleteAsync(room.Id))
            {
                ShowSuccess("Xóa phòng thành công!");
                await LoadAsync();
            }
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;

    private void ClearForm()
    {
        FormRoomCode = string.Empty;
        FormRoomName = string.Empty;
        FormAreaId = Areas.FirstOrDefault()?.Id ?? 0;
        FormArea = 0;
        FormPrice = 0;
        FormDeposit = 0;
        FormMaxOccupants = 2;
        FormStatus = RoomStatus.Available;
        FormDescription = string.Empty;
    }
}
