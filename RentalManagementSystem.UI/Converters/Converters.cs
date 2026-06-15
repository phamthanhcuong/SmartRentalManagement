using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

/// <summary>Số > 0 -> Visible, ngược lại Collapsed (ẩn cảnh báo khi không có gì cần xử lý).</summary>
public class PositiveNumberToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var n = System.Convert.ToDecimal(value ?? 0, CultureInfo.InvariantCulture);
        return n > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Đổi giá trị enum (hoặc trạng thái) sang nhãn tiếng Việt dễ hiểu.</summary>
public class EnumToVietnameseConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Map = new()
    {
        // Loại xe
        ["Motorbike"] = "Xe máy", ["Car"] = "Ô tô", ["ElectricBike"] = "Xe máy điện",
        ["Bicycle"] = "Xe đạp", ["Other"] = "Khác",
        // Loại dịch vụ
        ["Electric"] = "Điện", ["Water"] = "Nước", ["Internet"] = "Internet",
        ["Parking"] = "Giữ xe", ["Cleaning"] = "Vệ sinh", ["Security"] = "An ninh",
        // Trạng thái phòng
        ["Available"] = "Còn trống", ["Occupied"] = "Đang thuê",
        ["Maintenance"] = "Đang sửa", ["Reserved"] = "Đã đặt",
        // Trạng thái hợp đồng
        ["Active"] = "Hiệu lực", ["Expired"] = "Hết hạn",
        ["Terminated"] = "Đã thanh lý", ["Pending"] = "Chờ duyệt",
        // Trạng thái hóa đơn
        ["Unpaid"] = "Chưa thu", ["Paid"] = "Đã thu", ["Overdue"] = "Quá hạn",
        ["Cancelled"] = "Đã hủy", ["PartiallyPaid"] = "Trả một phần",
        // Thu chi
        ["Income"] = "Khoản thu", ["Expense"] = "Khoản chi",
        // Vai trò
        ["Admin"] = "Quản trị viên", ["Manager"] = "Quản lý", ["Staff"] = "Nhân viên",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return "";
        var key = value.ToString() ?? "";
        return Map.TryGetValue(key, out var vi) ? vi : key;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>True -> tỉ lệ phóng to (Chế độ Người Cao Tuổi), false -> 1.0.</summary>
public class BoolToScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var scale = 1.25;
        if (parameter is string s && double.TryParse(s, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) scale = p;
        return value is true ? scale : 1.0;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Khác null -> true (dùng bật/tắt nút thao tác khi có/không có dòng được chọn).</summary>
public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value != null && !string.IsNullOrEmpty(value.ToString()) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
                return value is true ? parts[0] : parts[1];
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class MenuHighlightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value?.ToString() == parameter?.ToString())
            return System.Windows.Media.Brushes.Transparent; // Will be overridden by active style
        return System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string paramStr)
            return value?.ToString() == paramStr;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramStr)
        {
            if (Enum.TryParse(typeof(TransactionType), paramStr, out var result))
                return result;
        }
        return Binding.DoNothing;
    }
}

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is decimal d ? $"{d:N0} ₫" : value?.ToString() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
