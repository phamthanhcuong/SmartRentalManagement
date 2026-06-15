using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.UI.ViewModels;

public partial class ContractViewModel : BaseViewModel
{
    private readonly IContractService _contractService;
    private readonly IRoomService _roomService;
    private readonly ITenantService _tenantService;
    private readonly IContractSubscriptionService _subscriptionService;
    private readonly IServiceService _serviceService;
    private readonly ILogger<ContractViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Contract> _contracts = new();
    [ObservableProperty] private ObservableCollection<Room> _rooms = new();
    [ObservableProperty] private ObservableCollection<Tenant> _tenants = new();
    [ObservableProperty] private Contract? _selectedContract;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isRenewDialogOpen;
    [ObservableProperty] private DateTime _renewEndDate = DateTime.Now.AddMonths(6);

    // Form fields
    [ObservableProperty] private string _formContractNo = string.Empty;
    [ObservableProperty] private int _formTenantId;
    [ObservableProperty] private int _formRoomId;
    [ObservableProperty] private DateTime _formStartDate = DateTime.Today;
    [ObservableProperty] private DateTime _formEndDate = DateTime.Today.AddMonths(6);
    [ObservableProperty] private decimal _formDepositAmount;
    [ObservableProperty] private decimal _formMonthlyRent;
    [ObservableProperty] private int _formPaymentDueDay = 5;
    [ObservableProperty] private int _formOccupantCount = 1;
    [ObservableProperty] private string _formTerms = string.Empty;
    [ObservableProperty] private string _formNote = string.Empty;

    // Subscription (dịch vụ theo hợp đồng) dialog
    [ObservableProperty] private bool _isSubscriptionDialogOpen;
    [ObservableProperty] private Contract? _subscriptionContract;
    [ObservableProperty] private ObservableCollection<ContractSubscription> _subscriptions = new();
    [ObservableProperty] private ObservableCollection<Domain.Entities.Service> _availableServices = new();
    [ObservableProperty] private int _subServiceId;
    [ObservableProperty] private decimal _subQuantity = 1;
    [ObservableProperty] private decimal _subUnitPrice;
    [ObservableProperty] private bool _subIsPerPerson;

    public ContractViewModel(IContractService contractService, IRoomService roomService,
        ITenantService tenantService, IContractSubscriptionService subscriptionService,
        IServiceService serviceService, ILogger<ContractViewModel> logger)
    {
        _contractService = contractService;
        _roomService = roomService;
        _tenantService = tenantService;
        _subscriptionService = subscriptionService;
        _serviceService = serviceService;
        _logger = logger;
    }

    // Khi chọn phòng lúc tạo HĐ: tự điền giá thuê & cọc theo cấu hình phòng (giảm thao tác nhập tay)
    partial void OnFormRoomIdChanged(int value)
    {
        if (IsEditing || value == 0) return;
        var room = Rooms.FirstOrDefault(r => r.Id == value);
        if (room == null) return;
        FormMonthlyRent = room.Price;
        FormDepositAmount = room.Deposit > 0 ? room.Deposit : room.Price; // mặc định cọc = 1 tháng nếu chưa cấu hình
        if (FormOccupantCount < 1) FormOccupantCount = 1;
    }

    // Khi chọn dịch vụ, tự điền đơn giá mặc định của dịch vụ
    partial void OnSubServiceIdChanged(int value)
    {
        var svc = AvailableServices.FirstOrDefault(s => s.Id == value);
        if (svc != null) SubUnitPrice = svc.UnitPrice;
    }

    [RelayCommand]
    private async Task OpenSubscriptionDialogAsync(Contract contract)
    {
        SubscriptionContract = contract;
        SubServiceId = 0; SubQuantity = 1; SubUnitPrice = 0; SubIsPerPerson = false;
        try
        {
            // Chỉ hiển thị dịch vụ định kỳ (không phải điện/nước - đã tính theo chỉ số)
            var services = (await _serviceService.GetAllAsync())
                .Where(s => s.IsActive && s.ServiceType != ServiceType.Electric && s.ServiceType != ServiceType.Water)
                .ToList();
            AvailableServices = new ObservableCollection<Domain.Entities.Service>(services);
            var subs = await _subscriptionService.GetByContractAsync(contract.Id);
            Subscriptions = new ObservableCollection<ContractSubscription>(subs);
            IsSubscriptionDialogOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening subscription dialog");
            ShowError("Lỗi tải dịch vụ hợp đồng.");
        }
    }

    [RelayCommand]
    private async Task AddSubscriptionAsync()
    {
        if (SubscriptionContract == null) return;
        if (SubServiceId == 0) { ShowError("Vui lòng chọn dịch vụ."); return; }
        if (SubUnitPrice <= 0) { ShowError("Đơn giá phải lớn hơn 0."); return; }

        var sub = new ContractSubscription
        {
            ContractId = SubscriptionContract.Id,
            ServiceId = SubServiceId,
            Quantity = SubQuantity <= 0 ? 1 : SubQuantity,
            UnitPrice = SubUnitPrice,
            IsPerPerson = SubIsPerPerson
        };
        if (await _subscriptionService.AddAsync(sub))
        {
            var subs = await _subscriptionService.GetByContractAsync(SubscriptionContract.Id);
            Subscriptions = new ObservableCollection<ContractSubscription>(subs);
            SubServiceId = 0; SubQuantity = 1; SubUnitPrice = 0; SubIsPerPerson = false;
        }
        else ShowError("Không thể thêm: dịch vụ đã được đăng ký hoặc lỗi dữ liệu.");
    }

    [RelayCommand]
    private async Task RemoveSubscriptionAsync(ContractSubscription sub)
    {
        if (await _subscriptionService.RemoveAsync(sub.Id) && SubscriptionContract != null)
        {
            var subs = await _subscriptionService.GetByContractAsync(SubscriptionContract.Id);
            Subscriptions = new ObservableCollection<ContractSubscription>(subs);
        }
    }

    [RelayCommand]
    private void CloseSubscriptionDialog() => IsSubscriptionDialogOpen = false;

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải hợp đồng...");
        try
        {
            var contracts = await _contractService.GetAllAsync();
            var rooms = await _roomService.GetAllAsync();
            var tenants = await _tenantService.GetAllAsync();
            Contracts = new ObservableCollection<Contract>(contracts);
            Rooms = new ObservableCollection<Room>(rooms);
            Tenants = new ObservableCollection<Tenant>(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading contracts");
            ShowError("Lỗi tải hợp đồng.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task OpenAddDialogAsync()
    {
        IsEditing = false;
        FormContractNo = await _contractService.GenerateContractNoAsync();
        FormTenantId = 0;
        FormRoomId = 0;
        FormStartDate = DateTime.Today;
        FormEndDate = DateTime.Today.AddMonths(6);
        FormDepositAmount = 0;
        FormMonthlyRent = 0;
        FormPaymentDueDay = 5;
        FormOccupantCount = 1;
        FormTerms = FormNote = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Contract contract)
    {
        IsEditing = true;
        SelectedContract = contract;
        FormContractNo = contract.ContractNo;
        FormTenantId = contract.TenantId;
        FormRoomId = contract.RoomId;
        FormStartDate = contract.StartDate;
        FormEndDate = contract.EndDate;
        FormDepositAmount = contract.DepositAmount;
        FormMonthlyRent = contract.MonthlyRent;
        FormPaymentDueDay = contract.PaymentDueDay;
        FormOccupantCount = contract.OccupantCount;
        FormTerms = contract.Terms ?? string.Empty;
        FormNote = contract.Note ?? string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (FormRoomId == 0 || FormTenantId == 0)
        {
            ShowError("Vui lòng chọn phòng và khách thuê."); return;
        }

        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedContract != null)
            {
                SelectedContract.StartDate = FormStartDate;
                SelectedContract.EndDate = FormEndDate;
                SelectedContract.DepositAmount = FormDepositAmount;
                SelectedContract.MonthlyRent = FormMonthlyRent;
                SelectedContract.PaymentDueDay = FormPaymentDueDay;
                SelectedContract.OccupantCount = FormOccupantCount;
                SelectedContract.Terms = FormTerms;
                SelectedContract.Note = FormNote;
                result = await _contractService.UpdateAsync(SelectedContract);
            }
            else
            {
                var contract = new Contract
                {
                    ContractNo = FormContractNo,
                    TenantId = FormTenantId,
                    RoomId = FormRoomId,
                    StartDate = FormStartDate,
                    EndDate = FormEndDate,
                    DepositAmount = FormDepositAmount,
                    MonthlyRent = FormMonthlyRent,
                    PaymentDueDay = FormPaymentDueDay,
                    OccupantCount = FormOccupantCount,
                    Terms = FormTerms,
                    Note = FormNote,
                    Status = ContractStatus.Active
                };
                result = await _contractService.CreateAsync(contract);
            }

            if (result)
            {
                ShowSuccess(IsEditing ? "Cập nhật hợp đồng thành công!" : "Tạo hợp đồng thành công!");
                IsDialogOpen = false;
                await LoadAsync();
            }
            else ShowError(IsEditing ? "Có lỗi xảy ra." : "Không thể tạo: phòng đã có hợp đồng hiệu lực hoặc dữ liệu không hợp lệ.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving contract");
            ShowError("Lỗi khi lưu hợp đồng.");
        }
        finally { SetBusy(false); }
    }

    // ===== Trả phòng (Checkout) =====
    [ObservableProperty] private bool _isCheckoutDialogOpen;
    [ObservableProperty] private Contract? _checkoutContract;
    [ObservableProperty] private decimal _checkoutOutstanding;
    [ObservableProperty] private decimal _checkoutDeduction;

    public decimal CheckoutRefund => Math.Max(0, (CheckoutContract?.DepositAmount ?? 0) - CheckoutDeduction);
    partial void OnCheckoutDeductionChanged(decimal value) => OnPropertyChanged(nameof(CheckoutRefund));

    [RelayCommand]
    private async Task OpenCheckoutDialogAsync(Contract contract)
    {
        CheckoutContract = contract;
        CheckoutOutstanding = await _contractService.GetOutstandingByContractAsync(contract.Id);
        // Gợi ý khấu trừ công nợ còn lại vào tiền cọc
        CheckoutDeduction = Math.Min(CheckoutOutstanding, contract.DepositAmount);
        OnPropertyChanged(nameof(CheckoutRefund));
        IsCheckoutDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmCheckoutAsync()
    {
        if (CheckoutContract == null) return;
        if (await _contractService.CheckoutAsync(CheckoutContract.Id, CheckoutDeduction))
        {
            ShowSuccess("Đã trả phòng & thanh lý hợp đồng.");
            IsCheckoutDialogOpen = false;
            await LoadAsync();
        }
        else ShowError("Không thể trả phòng.");
    }

    [RelayCommand]
    private void CloseCheckoutDialog() => IsCheckoutDialogOpen = false;

    // ===== Nhường phòng (Transfer) =====
    [ObservableProperty] private bool _isTransferDialogOpen;
    [ObservableProperty] private Contract? _transferContract;
    [ObservableProperty] private int _transferNewTenantId;
    [ObservableProperty] private decimal _transferNewDeposit;
    [ObservableProperty] private bool _transferKeepDeposit = true;

    [RelayCommand]
    private void OpenTransferDialog(Contract contract)
    {
        TransferContract = contract;
        TransferNewTenantId = 0;
        TransferKeepDeposit = true;
        TransferNewDeposit = contract.DepositAmount;
        IsTransferDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmTransferAsync()
    {
        if (TransferContract == null) return;
        if (TransferNewTenantId == 0) { ShowError("Vui lòng chọn khách nhận phòng."); return; }
        if (TransferNewTenantId == TransferContract.TenantId) { ShowError("Khách nhận phải khác khách hiện tại."); return; }

        if (await _contractService.TransferAsync(TransferContract.Id, TransferNewTenantId, TransferNewDeposit, TransferKeepDeposit))
        {
            ShowSuccess("Đã nhường phòng cho khách mới (tạo HĐ mới, sao chép dịch vụ).");
            IsTransferDialogOpen = false;
            await LoadAsync();
        }
        else ShowError("Không thể nhường phòng.");
    }

    [RelayCommand]
    private void CloseTransferDialog() => IsTransferDialogOpen = false;

    [RelayCommand]
    private void OpenRenewDialog(Contract contract)
    {
        SelectedContract = contract;
        RenewEndDate = contract.EndDate.AddMonths(6);
        IsRenewDialogOpen = true;
    }

    [RelayCommand]
    private async Task RenewAsync()
    {
        if (SelectedContract == null) return;
        if (await _contractService.RenewAsync(SelectedContract.Id, RenewEndDate))
        {
            ShowSuccess("Gia hạn hợp đồng thành công!");
            IsRenewDialogOpen = false;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private void CloseDialog() { IsDialogOpen = false; IsRenewDialogOpen = false; }
}
