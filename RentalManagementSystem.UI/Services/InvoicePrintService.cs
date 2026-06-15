using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.UI.Services;

/// <summary>
/// In hóa đơn: ưu tiên máy in vật lý mặc định; nếu không có thì in ra PDF
/// (qua hàng đợi "Microsoft Print to PDF" của Windows).
/// </summary>
public class InvoicePrintService
{
    private readonly IInvoiceService _invoiceService;
    private readonly ISystemSettingService _settingService;

    public InvoicePrintService(IInvoiceService invoiceService, ISystemSettingService settingService)
    {
        _invoiceService = invoiceService;
        _settingService = settingService;
    }

    public record PrintOutcome(bool Success, bool UsedPhysicalPrinter, string PrinterName, string? Message);

    public async Task<PrintOutcome> PrintInvoiceAsync(int invoiceId)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId);
        if (invoice == null) return new PrintOutcome(false, false, "", "Không tìm thấy hóa đơn.");

        var (queue, isPhysical) = ResolvePrintQueue();
        if (queue == null)
            return new PrintOutcome(false, false, "", "Không tìm thấy máy in hoặc trình in PDF.");

        var setting = await _settingService.GetAsync();
        var doc = BuildDocument(invoice, setting);
        var pd = new PrintDialog { PrintQueue = queue };

        // Khổ A4 dọc
        doc.PageHeight = pd.PrintableAreaHeight;
        doc.PageWidth = pd.PrintableAreaWidth;
        doc.ColumnWidth = pd.PrintableAreaWidth;

        IDocumentPaginatorSource idp = doc;
        pd.PrintDocument(idp.DocumentPaginator, $"Hóa đơn {invoice.InvoiceNo}");

        return new PrintOutcome(true, isPhysical, queue.Name, null);
    }

    /// <summary>Chọn hàng đợi in: máy in vật lý mặc định nếu có, ngược lại "Microsoft Print to PDF".</summary>
    private static (PrintQueue? queue, bool isPhysical) ResolvePrintQueue()
    {
        PrintQueue? defaultQueue = null;
        try { defaultQueue = LocalPrintServer.GetDefaultPrintQueue(); } catch { }

        if (defaultQueue != null && !IsVirtual(defaultQueue.Name))
            return (defaultQueue, true);

        // Không có máy in vật lý -> tìm trình in PDF của Windows
        try
        {
            using var server = new LocalPrintServer();
            var queues = server.GetPrintQueues(new[]
            {
                EnumeratedPrintQueueTypes.Local,
                EnumeratedPrintQueueTypes.Connections
            });
            var pdf = queues.FirstOrDefault(q => q.Name.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0)
                   ?? queues.FirstOrDefault(q => q.Name.IndexOf("XPS", StringComparison.OrdinalIgnoreCase) >= 0);
            if (pdf != null) return (pdf, false);
        }
        catch { }

        // Cùng lắm trả về máy in mặc định (kể cả ảo) nếu có
        return (defaultQueue, defaultQueue != null && !IsVirtual(defaultQueue.Name));
    }

    private static bool IsVirtual(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("pdf") || n.Contains("xps") || n.Contains("onenote") || n.Contains("fax");
    }

    private static readonly SolidColorBrush Primary = new(Color.FromRgb(0x7A, 0x1E, 0x3A));
    private static readonly SolidColorBrush PrimaryDark = new(Color.FromRgb(0x4E, 0x0F, 0x25));
    private static readonly SolidColorBrush Muted = new(Color.FromRgb(0x75, 0x75, 0x75));
    private static readonly SolidColorBrush LightRow = new(Color.FromRgb(0xF7, 0xE9, 0xEE));

    private static FlowDocument BuildDocument(Invoice inv, SystemSetting cfg)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            PagePadding = new Thickness(48),
            Background = Brushes.White
        };

        // Tên & thông tin chủ trọ (từ Cấu hình hệ thống)
        doc.Blocks.Add(new Paragraph(new Run(cfg.CompanyName?.ToUpper() ?? "NHÀ TRỌ"))
        {
            FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Primary,
            TextAlignment = TextAlignment.Center, Margin = new Thickness(0)
        });
        var contact = string.Join("  •  ", new[] { cfg.Address, cfg.Phone }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(contact))
            doc.Blocks.Add(new Paragraph(new Run(contact))
            { FontSize = 11, Foreground = Muted, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) });

        // Tiêu đề
        var title = new Paragraph(new Run("HÓA ĐƠN THANH TOÁN"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = PrimaryDark,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        doc.Blocks.Add(title);

        var sub = new Paragraph(new Run($"Số: {inv.InvoiceNo}  •  Kỳ: {inv.Month:D2}/{inv.Year}"))
        {
            FontSize = 12,
            Foreground = Muted,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        doc.Blocks.Add(sub);

        // Thông tin khách / phòng
        var info = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 14) };
        info.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        info.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var irg = new TableRowGroup();
        var ir = new TableRow();
        ir.Cells.Add(InfoCell("Khách thuê", inv.Tenant?.FullName ?? ""));
        ir.Cells.Add(InfoCell("Phòng", inv.Room?.RoomCode ?? ""));
        irg.Rows.Add(ir);
        var ir2 = new TableRow();
        ir2.Cells.Add(InfoCell("Ngày lập", inv.InvoiceDate.ToString("dd/MM/yyyy")));
        ir2.Cells.Add(InfoCell("Hạn thanh toán", inv.DueDate?.ToString("dd/MM/yyyy") ?? "—"));
        irg.Rows.Add(ir2);
        info.RowGroups.Add(irg);
        doc.Blocks.Add(info);

        // Bảng chi tiết
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
        table.Columns.Add(new TableColumn { Width = new GridLength(4, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.6, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.8, GridUnitType.Star) });

        var header = new TableRowGroup();
        var hr = new TableRow { Background = Primary };
        hr.Cells.Add(HeadCell("Khoản mục", TextAlignment.Left));
        hr.Cells.Add(HeadCell("SL", TextAlignment.Right));
        hr.Cells.Add(HeadCell("Đơn giá", TextAlignment.Right));
        hr.Cells.Add(HeadCell("Thành tiền", TextAlignment.Right));
        header.Rows.Add(hr);
        table.RowGroups.Add(header);

        var body = new TableRowGroup();
        bool alt = false;
        if (inv.InvoiceDetails != null && inv.InvoiceDetails.Count > 0)
        {
            foreach (var d in inv.InvoiceDetails)
            {
                var row = new TableRow { Background = alt ? LightRow : Brushes.White };
                row.Cells.Add(BodyCell(d.ItemName, TextAlignment.Left));
                row.Cells.Add(BodyCell($"{d.Quantity:N0}", TextAlignment.Right));
                row.Cells.Add(BodyCell($"{d.UnitPrice:N0}", TextAlignment.Right));
                row.Cells.Add(BodyCell($"{d.Amount:N0} ₫", TextAlignment.Right));
                body.Rows.Add(row);
                alt = !alt;
            }
        }
        else
        {
            // Trường hợp hóa đơn không có chi tiết: liệt kê theo các cột tổng
            AddSimpleRow(body, "Tiền thuê phòng", inv.RentAmount, ref alt);
            AddSimpleRow(body, "Tiền điện", inv.ElectricAmount, ref alt);
            AddSimpleRow(body, "Tiền nước", inv.WaterAmount, ref alt);
            AddSimpleRow(body, "Dịch vụ", inv.ServiceAmount, ref alt);
        }
        table.RowGroups.Add(body);
        doc.Blocks.Add(table);

        // Tổng kết
        doc.Blocks.Add(TotalLine("Nợ kỳ trước", inv.PreviousDebt, false));
        doc.Blocks.Add(TotalLine("Giảm trừ", -inv.DiscountAmount, false));
        doc.Blocks.Add(TotalLine("TỔNG CỘNG", inv.TotalAmount, true));
        doc.Blocks.Add(TotalLine("Đã thanh toán", inv.PaidAmount, false));
        doc.Blocks.Add(TotalLine("Còn phải thu", inv.TotalAmount - inv.PaidAmount, true));

        // Thông tin chuyển khoản (nếu có cấu hình)
        if (!string.IsNullOrWhiteSpace(cfg.BankAccount))
        {
            doc.Blocks.Add(new Paragraph(new Run($"Chuyển khoản: {cfg.BankName} - {cfg.BankAccount}"))
            { FontSize = 12, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 16, 0, 0) });
        }

        // Chân trang
        var foot = new Paragraph(new Run(string.IsNullOrWhiteSpace(cfg.InvoiceFooterNote) ? "Cảm ơn Quý khách!" : cfg.InvoiceFooterNote))
        {
            FontSize = 11,
            Foreground = Muted,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };
        doc.Blocks.Add(foot);

        return doc;
    }

    private static void AddSimpleRow(TableRowGroup body, string name, decimal amount, ref bool alt)
    {
        if (amount == 0) return;
        var row = new TableRow { Background = alt ? LightRow : Brushes.White };
        row.Cells.Add(BodyCell(name, TextAlignment.Left));
        row.Cells.Add(BodyCell("1", TextAlignment.Right));
        row.Cells.Add(BodyCell($"{amount:N0}", TextAlignment.Right));
        row.Cells.Add(BodyCell($"{amount:N0} ₫", TextAlignment.Right));
        body.Rows.Add(row);
        alt = !alt;
    }

    private static TableCell InfoCell(string label, string value)
    {
        var p = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
        p.Inlines.Add(new Run(label + ": ") { Foreground = Muted });
        p.Inlines.Add(new Run(value) { FontWeight = FontWeights.SemiBold });
        return new TableCell(p) { Padding = new Thickness(2) };
    }

    private static TableCell HeadCell(string text, TextAlignment align) =>
        new(new Paragraph(new Run(text)) { Foreground = Brushes.White, FontWeight = FontWeights.Bold, TextAlignment = align })
        { Padding = new Thickness(8, 6, 8, 6) };

    private static TableCell BodyCell(string text, TextAlignment align) =>
        new(new Paragraph(new Run(text)) { TextAlignment = align })
        { Padding = new Thickness(8, 5, 8, 5) };

    private static Paragraph TotalLine(string label, decimal value, bool bold)
    {
        var p = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 1, 0, 1),
            FontSize = bold ? 14 : 12,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = bold ? PrimaryDark : Brushes.Black
        };
        p.Inlines.Add(new Run($"{label}:  ") { Foreground = bold ? PrimaryDark : Muted });
        p.Inlines.Add(new Run($"{value:N0} ₫"));
        return p;
    }
}
