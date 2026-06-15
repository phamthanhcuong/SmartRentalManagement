using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.UI.ViewModels;

public partial class IncomeExpenseViewModel : BaseViewModel
{
    private readonly IIncomeExpenseService _service;
    private readonly ILogger<IncomeExpenseViewModel> _logger;

    [ObservableProperty] private ObservableCollection<IncomeExpense> _items = new();
    [ObservableProperty] private IncomeExpense? _selectedItem;
    [ObservableProperty] private int _filterMonth = DateTime.Now.Month;
    [ObservableProperty] private int _filterYear = DateTime.Now.Year;
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private decimal _profit;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditing;

    // Form
    [ObservableProperty] private TransactionType _formType = TransactionType.Income;
    [ObservableProperty] private string _formCategory = string.Empty;
    [ObservableProperty] private decimal _formAmount;
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private DateTime _formDate = DateTime.Today;

    public IncomeExpenseViewModel(IIncomeExpenseService service, ILogger<IncomeExpenseViewModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        SetBusy(true, "Đang tải dữ liệu thu chi...");
        try
        {
            var items = await _service.GetByMonthYearAsync(FilterMonth, FilterYear);
            Items = new ObservableCollection<IncomeExpense>(items);
            TotalIncome = await _service.GetTotalIncomeAsync(FilterMonth, FilterYear);
            TotalExpense = await _service.GetTotalExpenseAsync(FilterMonth, FilterYear);
            Profit = TotalIncome - TotalExpense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading income/expense");
            ShowError("Lỗi tải dữ liệu thu chi.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task FilterAsync() => await LoadAsync();

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditing = false;
        FormType = TransactionType.Income;
        FormCategory = FormDescription = string.Empty;
        FormAmount = 0;
        FormDate = DateTime.Today;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(IncomeExpense item)
    {
        IsEditing = true;
        SelectedItem = item;
        FormType = item.Type;
        FormCategory = item.Category;
        FormAmount = item.Amount;
        FormDescription = item.Description;
        FormDate = item.TransactionDate;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCategory)) { ShowError("Vui lòng nhập danh mục."); return; }
        if (FormAmount <= 0) { ShowError("Số tiền phải lớn hơn 0."); return; }

        SetBusy(true);
        try
        {
            bool result;
            if (IsEditing && SelectedItem != null)
            {
                SelectedItem.Type = FormType;
                SelectedItem.Category = FormCategory;
                SelectedItem.Amount = FormAmount;
                SelectedItem.Description = FormDescription;
                SelectedItem.TransactionDate = FormDate;
                result = await _service.UpdateAsync(SelectedItem);
            }
            else
            {
                result = await _service.CreateAsync(new IncomeExpense
                {
                    Type = FormType,
                    Category = FormCategory,
                    Amount = FormAmount,
                    Description = FormDescription,
                    TransactionDate = FormDate
                });
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
    private async Task DeleteAsync(IncomeExpense item)
    {
        if (!ConfirmDelete($"giao dịch \"{item.Description}\" ({item.Amount:N0}đ)")) return;
        if (await _service.DeleteAsync(item.Id)) await LoadAsync();
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;
}
