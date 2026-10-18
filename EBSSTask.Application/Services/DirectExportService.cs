using ClosedXML.Excel;
using EBSSTask.Application.DTOs.BalanceHistory;
using EBSSTask.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EBSSTask.Application.Services;

public sealed class DirectExportService : IDirectExportService
{
    public DirectExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    public byte[] GenerateDirectPdf(IEnumerable<BalanceHistoryDto> items, BalanceHistoryQueryParameters parameters)
    {
        var itemList = items as IReadOnlyList<BalanceHistoryDto> ?? items.ToList();
        var pageDebtor = itemList.Sum(x => x.Debtor ?? 0);
        var pageCreditor = itemList.Sum(x => x.Creditor ?? 0);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4); // A4 Portrait
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.5f).FontColor("#1E293B"));

                // Page Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text("EBSS Account Statement").FontSize(15).Bold().FontColor("#0F4C81");
                            var filterText = $"Account ID: {parameters.BalanceId?.ToString() ?? "All"}";
                            if (parameters.FromDate.HasValue || parameters.ToDate.HasValue)
                            {
                                filterText += $" | Date: {parameters.FromDate:yyyy-MM-dd} to {parameters.ToDate:yyyy-MM-dd}";
                            }
                            if (!string.IsNullOrWhiteSpace(parameters.Search))
                            {
                                filterText += $" | Search: \"{parameters.Search}\"";
                            }
                            titleCol.Item().Text(filterText).FontSize(9).FontColor("#475569");
                        });

                        row.ConstantItem(180).AlignRight().Column(metaCol =>
                        {
                            metaCol.Item().Text($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#64748B");
                            metaCol.Item().Text($"Page {parameters.PageNumber} · Records: {itemList.Count}").FontSize(8).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingTop(6).PaddingBottom(10).LineHorizontal(1).LineColor("#E3E7EC");
                });

                // Content Table (Transactions only, no remarks, no ref no, no data summary cards)
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);   // ID
                        columns.ConstantColumn(60);   // Account ID
                        columns.ConstantColumn(65);   // Date
                        columns.RelativeColumn(1.2f); // Order / Bill
                        columns.RelativeColumn(1.1f); // Prev Balance
                        columns.RelativeColumn(1.1f); // Debit
                        columns.RelativeColumn(1.1f); // Credit
                        columns.RelativeColumn(1.1f); // Final Balance
                    });

                    // Header Row
                    table.Header(header =>
                    {
                        header.Cell().Background("#0F4C81").Padding(5).Text("ID").FontSize(8).Bold().FontColor(Colors.White);
                        header.Cell().Background("#0F4C81").Padding(5).Text("ACCOUNT").FontSize(8).Bold().FontColor(Colors.White);
                        header.Cell().Background("#0F4C81").Padding(5).Text("DATE").FontSize(8).Bold().FontColor(Colors.White);
                        header.Cell().Background("#0F4C81").Padding(5).Text("ORDER/BILL").FontSize(8).Bold().FontColor(Colors.White);
                        header.Cell().Background("#0F4C81").Padding(5).AlignRight().Text("PREV BAL").FontSize(8).Bold().FontColor(Colors.White);
                        header.Cell().Background("#0F4C81").Padding(5).AlignRight().Text("DEBIT").FontSize(8).Bold().FontColor(Colors.White);
                        header.Cell().Background("#0F4C81").Padding(5).AlignRight().Text("CREDIT").FontSize(8).Bold().FontColor(Colors.White);
                        header.Cell().Background("#0F4C81").Padding(5).AlignRight().Text("FINAL BAL").FontSize(8).Bold().FontColor(Colors.White);
                    });

                    // Data Rows
                    var isAlt = false;
                    foreach (var item in itemList)
                    {
                        var bg = isAlt ? "#F8FAFC" : "#FFFFFF";
                        isAlt = !isAlt;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).Text($"{item.BalanceHisId}").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).Text($"{item.BalanceId}").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).Text(item.Date.HasValue ? item.Date.Value.ToString("yyyy-MM-dd") : "-").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).Text(item.OrderBill ?? "-").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).AlignRight().Text(item.PrevBalnce.HasValue ? $"{item.PrevBalnce.Value:N2}" : "0.00").FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).AlignRight().Text((item.Debtor ?? 0) > 0 ? $"{(item.Debtor ?? 0):N2}" : "-").FontSize(7.5f).FontColor((item.Debtor ?? 0) > 0 ? "#1A4FAE" : "#64748B");
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).AlignRight().Text((item.Creditor ?? 0) > 0 ? $"{(item.Creditor ?? 0):N2}" : "-").FontSize(7.5f).FontColor((item.Creditor ?? 0) > 0 ? "#A84A00" : "#64748B");
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E3E7EC").Padding(4).AlignRight().Text(item.NewBalnce.HasValue ? $"{item.NewBalnce.Value:N2}" : "-").FontSize(7.5f).Bold().FontColor("#0F2A43");
                    }

                    // Footer Row (Totals)
                    table.Footer(footer =>
                    {
                        footer.Cell().ColumnSpan(5).Background("#F1F4F8").Padding(5).Text("TOTALS").FontSize(8).Bold().FontColor("#4F5D6B");
                        footer.Cell().Background("#F1F4F8").Padding(5).AlignRight().Text($"{pageDebtor:N2}").FontSize(8).Bold().FontColor("#1A4FAE");
                        footer.Cell().Background("#F1F4F8").Padding(5).AlignRight().Text($"{pageCreditor:N2}").FontSize(8).Bold().FontColor("#A84A00");
                        footer.Cell().Background("#F1F4F8").Padding(5).Text(string.Empty);
                    });
                });

                // Page Footer
                page.Footer().Row(fRow =>
                {
                    fRow.RelativeItem().Text("EBSS Statement Page Export · Generated instantaneously").FontSize(7.5f).FontColor("#94A3B8");
                    fRow.ConstantItem(100).AlignRight().Text(x =>
                    {
                        x.CurrentPageNumber().FontSize(7.5f).FontColor("#94A3B8");
                        x.Span(" / ").FontSize(7.5f).FontColor("#94A3B8");
                        x.TotalPages().FontSize(7.5f).FontColor("#94A3B8");
                    });
                });
            });
        });

        using var memoryStream = new MemoryStream();
        document.GeneratePdf(memoryStream);
        return memoryStream.ToArray();
    }

    public byte[] GenerateDirectExcel(IEnumerable<BalanceHistoryDto> items, BalanceHistoryQueryParameters parameters)
    {
        var itemList = items as IReadOnlyList<BalanceHistoryDto> ?? items.ToList();
        var pageDebtor = itemList.Sum(x => x.Debtor ?? 0);
        var pageCreditor = itemList.Sum(x => x.Creditor ?? 0);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Statement Page");

        // Header Title
        worksheet.Cell(1, 1).Value = "EBSS Account Statement - Transactions Page";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(15, 76, 129);

        var filterText = $"Account ID: {parameters.BalanceId?.ToString() ?? "All"}";
        if (parameters.FromDate.HasValue || parameters.ToDate.HasValue)
        {
            filterText += $" | Date: {parameters.FromDate:yyyy-MM-dd} to {parameters.ToDate:yyyy-MM-dd}";
        }
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            filterText += $" | Search: \"{parameters.Search}\"";
        }
        filterText += $" | Export Date: {DateTime.Now:yyyy-MM-dd HH:mm} | Page: {parameters.PageNumber}";

        worksheet.Cell(2, 1).Value = filterText;
        worksheet.Cell(2, 1).Style.Font.FontSize = 9;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(100, 116, 139);

        // Table Headers (Row 4) - No summary KPIs, no reference no, no remarks
        var headers = new[]
        {
            "ID",
            "Account ID",
            "Date",
            "Order / Bill",
            "Previous Balance",
            "Debit Amount",
            "Credit Amount",
            "Final Balance"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(15, 76, 129);
            cell.Style.Alignment.Horizontal = (i >= 4) ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
        }

        // Table Rows
        var currentRow = 5;
        foreach (var item in itemList)
        {
            worksheet.Cell(currentRow, 1).Value = item.BalanceHisId;
            worksheet.Cell(currentRow, 2).Value = item.BalanceId;
            worksheet.Cell(currentRow, 3).Value = item.Date.HasValue ? item.Date.Value.ToString("yyyy-MM-dd") : string.Empty;
            worksheet.Cell(currentRow, 4).Value = item.OrderBill ?? string.Empty;

            worksheet.Cell(currentRow, 5).Value = item.PrevBalnce ?? 0;
            worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";

            worksheet.Cell(currentRow, 6).Value = item.Debtor ?? 0;
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";

            worksheet.Cell(currentRow, 7).Value = item.Creditor ?? 0;
            worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.00";

            worksheet.Cell(currentRow, 8).Value = item.NewBalnce ?? 0;
            worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(currentRow, 8).Style.Font.Bold = true;

            currentRow++;
        }

        // Footer Row (Totals)
        worksheet.Cell(currentRow, 1).Value = "TOTALS";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;

        worksheet.Cell(currentRow, 6).Value = pageDebtor;
        worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 6).Style.Font.FontColor = XLColor.FromArgb(26, 79, 174);

        worksheet.Cell(currentRow, 7).Value = pageCreditor;
        worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Cell(currentRow, 7).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 7).Style.Font.FontColor = XLColor.FromArgb(168, 74, 0);

        worksheet.Range(currentRow, 1, currentRow, headers.Length).Style.Fill.BackgroundColor = XLColor.FromArgb(241, 244, 248);

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }
}
