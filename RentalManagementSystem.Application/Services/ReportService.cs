using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork uow, ILogger<ReportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task ExportRevenueToExcelAsync(int month, int year, string filePath)
    {
        var invoices = await _uow.Invoices.Query()
            .Include(i => i.Room)
            .Include(i => i.Tenant)
            .Where(i => i.Month == month && i.Year == year)
            .OrderBy(i => i.Room.RoomCode)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Doanh thu {month:D2}-{year}");

        // Title
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = $"BÁO CÁO DOANH THU THÁNG {month:D2}/{year}";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, 8).Merge();

        // Headers
        var headers = new[] { "STT", "Mã phòng", "Khách thuê", "Tiền thuê", "Tiền điện", "Tiền nước", "Tổng tiền", "Trạng thái" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976D2");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 4;
        decimal total = 0;
        for (int i = 0; i < invoices.Count; i++)
        {
            var inv = invoices[i];
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = inv.Room.RoomCode;
            ws.Cell(row, 3).Value = inv.Tenant.FullName;
            ws.Cell(row, 4).Value = inv.RentAmount;
            ws.Cell(row, 5).Value = inv.ElectricAmount;
            ws.Cell(row, 6).Value = inv.WaterAmount;
            ws.Cell(row, 7).Value = inv.TotalAmount;
            ws.Cell(row, 8).Value = inv.Status switch
            {
                InvoiceStatus.Paid => "Đã thanh toán",
                InvoiceStatus.Unpaid => "Chưa thanh toán",
                InvoiceStatus.Overdue => "Quá hạn",
                _ => "Đã hủy"
            };
            total += inv.TotalAmount;
            if (i % 2 == 1)
                ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            row++;
        }

        // Total row
        ws.Cell(row, 6).Value = "TỔNG CỘNG:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = total;
        ws.Cell(row, 7).Style.Font.Bold = true;

        // Format currency columns
        for (int c = 4; c <= 7; c++)
            ws.Range(4, c, row, c).Style.NumberFormat.Format = "#,##0";

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
        _logger.LogInformation("Revenue report exported to {Path}", filePath);
    }

    public async Task ExportDebtToExcelAsync(string filePath)
    {
        var invoices = await _uow.Invoices.Query()
            .Include(i => i.Room)
            .Include(i => i.Tenant)
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue || i.Status == InvoiceStatus.PartiallyPaid)
            .OrderBy(i => i.Room.RoomCode)
            .ToListAsync();

        var today = DateTime.Today;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Báo cáo công nợ");

        ws.Cell(1, 1).Value = "BÁO CÁO CÔNG NỢ (PHÂN TÍCH TUỔI NỢ)";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, 9).Merge();

        var headers = new[] { "STT", "Mã phòng", "Khách thuê", "SĐT", "Tháng/Năm", "Hạn TT", "Số nợ", "Số ngày quá hạn", "Nhóm tuổi nợ" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D32F2F");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Tổng hợp theo nhóm tuổi nợ
        decimal bucketCurrent = 0, bucket1_30 = 0, bucket31_60 = 0, bucket60Plus = 0;

        int row = 4;
        for (int i = 0; i < invoices.Count; i++)
        {
            var inv = invoices[i];
            var debt = inv.TotalAmount - inv.PaidAmount;
            var daysOverdue = inv.DueDate.HasValue ? Math.Max(0, (today - inv.DueDate.Value.Date).Days) : 0;
            string bucket;
            if (daysOverdue <= 0) { bucket = "Trong hạn"; bucketCurrent += debt; }
            else if (daysOverdue <= 30) { bucket = "1-30 ngày"; bucket1_30 += debt; }
            else if (daysOverdue <= 60) { bucket = "31-60 ngày"; bucket31_60 += debt; }
            else { bucket = "Trên 60 ngày"; bucket60Plus += debt; }

            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = inv.Room.RoomCode;
            ws.Cell(row, 3).Value = inv.Tenant.FullName;
            ws.Cell(row, 4).Value = inv.Tenant.Phone;
            ws.Cell(row, 5).Value = $"{inv.Month:D2}/{inv.Year}";
            ws.Cell(row, 6).Value = inv.DueDate?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(row, 7).Value = debt;
            ws.Cell(row, 8).Value = daysOverdue;
            ws.Cell(row, 9).Value = bucket;
            row++;
        }

        ws.Range(4, 7, row - 1, 7).Style.NumberFormat.Format = "#,##0";

        // Bảng tổng hợp tuổi nợ
        row += 1;
        ws.Cell(row, 2).Value = "TỔNG HỢP TUỔI NỢ"; ws.Cell(row, 2).Style.Font.Bold = true;
        row++;
        var summary = new (string Label, decimal Value)[]
        {
            ("Trong hạn", bucketCurrent), ("1-30 ngày", bucket1_30),
            ("31-60 ngày", bucket31_60), ("Trên 60 ngày", bucket60Plus),
            ("TỔNG CỘNG", bucketCurrent + bucket1_30 + bucket31_60 + bucket60Plus)
        };
        foreach (var (label, value) in summary)
        {
            ws.Cell(row, 2).Value = label;
            ws.Cell(row, 3).Value = value;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            if (label == "TỔNG CỘNG") { ws.Cell(row, 2).Style.Font.Bold = true; ws.Cell(row, 3).Style.Font.Bold = true; }
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    public async Task ExportUtilityToExcelAsync(int month, int year, string filePath)
    {
        var readings = await _uow.UtilityReadings.Query()
            .Include(u => u.Room)
            .Where(u => u.Month == month && u.Year == year)
            .OrderBy(u => u.Room.RoomCode)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Điện nước {month:D2}-{year}");

        ws.Cell(1, 1).Value = $"BÁO CÁO ĐIỆN NƯỚC THÁNG {month:D2}/{year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, 9).Merge();

        var headers = new[] { "STT", "Phòng", "Điện cũ", "Điện mới", "Tiêu thụ (kWh)", "Tiền điện", "Nước cũ", "Nước mới", "Tiền nước" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#388E3C");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 4;
        for (int i = 0; i < readings.Count; i++)
        {
            var r = readings[i];
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = r.Room.RoomCode;
            ws.Cell(row, 3).Value = r.ElectricOld;
            ws.Cell(row, 4).Value = r.ElectricNew;
            ws.Cell(row, 5).Value = r.ElectricNew - r.ElectricOld;
            ws.Cell(row, 6).Value = r.ElectricAmount;
            ws.Cell(row, 7).Value = r.WaterOld;
            ws.Cell(row, 8).Value = r.WaterNew;
            ws.Cell(row, 9).Value = r.WaterAmount;
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    public async Task PrintInvoiceAsync(int invoiceId, string filePath)
    {
        var invoice = await _uow.Invoices.Query()
            .Include(i => i.Room).ThenInclude(r => r.RentalArea)
            .Include(i => i.Tenant)
            .Include(i => i.InvoiceDetails)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) return;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Hóa đơn");

        ws.Cell(1, 1).Value = "HÓA ĐƠN TIỀN PHÒNG";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 18;
        ws.Range(1, 1, 1, 4).Merge();

        ws.Cell(3, 1).Value = $"Số hóa đơn: {invoice.InvoiceNo}";
        ws.Cell(4, 1).Value = $"Ngày: {invoice.InvoiceDate:dd/MM/yyyy}";
        ws.Cell(5, 1).Value = $"Phòng: {invoice.Room.RoomCode} - {invoice.Room.RentalArea.AreaName}";
        ws.Cell(6, 1).Value = $"Khách thuê: {invoice.Tenant.FullName}";
        ws.Cell(7, 1).Value = $"SĐT: {invoice.Tenant.Phone}";
        ws.Cell(8, 1).Value = $"Tháng: {invoice.Month:D2}/{invoice.Year}";

        ws.Cell(10, 1).Value = "Khoản mục";
        ws.Cell(10, 2).Value = "Số lượng";
        ws.Cell(10, 3).Value = "Đơn giá";
        ws.Cell(10, 4).Value = "Thành tiền";
        ws.Range(10, 1, 10, 4).Style.Font.Bold = true;
        ws.Range(10, 1, 10, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 11;
        ws.Cell(row, 1).Value = "Tiền thuê phòng"; ws.Cell(row, 4).Value = invoice.RentAmount; row++;
        if (invoice.ElectricAmount > 0) { ws.Cell(row, 1).Value = "Tiền điện"; ws.Cell(row, 4).Value = invoice.ElectricAmount; row++; }
        if (invoice.WaterAmount > 0) { ws.Cell(row, 1).Value = "Tiền nước"; ws.Cell(row, 4).Value = invoice.WaterAmount; row++; }
        if (invoice.ServiceAmount > 0) { ws.Cell(row, 1).Value = "Dịch vụ khác"; ws.Cell(row, 4).Value = invoice.ServiceAmount; row++; }

        ws.Cell(row, 3).Value = "TỔNG CỘNG:";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = invoice.TotalAmount;
        ws.Cell(row, 4).Style.Font.Bold = true;

        ws.Range(11, 4, row, 4).Style.NumberFormat.Format = "#,##0 VNĐ";
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
        _logger.LogInformation("Invoice {No} printed to {Path}", invoice.InvoiceNo, filePath);
    }
}
