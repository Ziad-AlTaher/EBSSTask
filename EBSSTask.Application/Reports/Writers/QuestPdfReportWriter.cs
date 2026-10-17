using EBSSTask.Application.Common.Options;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EBSSTask.Application.Reports.Writers;

public sealed class QuestPdfReportWriter : IReportWriter
{
    private readonly ReportGenerationOptions _options;

    public QuestPdfReportWriter(IOptions<ReportGenerationOptions> options)
    {
        _options = options.Value;
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    public ReportFormat Format => ReportFormat.Pdf;

    public async Task WriteAsync(
        IReportDataSource dataSource,
        ReportFilter filter,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        var totalCount = await dataSource.GetTotalCountAsync(filter, cancellationToken);

        if (totalCount > _options.MaxPdfRows)
        {
            throw new ReportGenerationException(
                $"PDF generation is limited to {_options.MaxPdfRows:N0} records to prevent high memory usage. " +
                $"Current report contains {totalCount:N0} records. Please export as Excel (.xlsx) instead.");
        }

        // Fetch Summary (via Stored Procedure for BalanceHistory)
        var summary = await dataSource.GetSummaryAsync(filter, cancellationToken);

        var rows = new List<IReadOnlyList<object?>>(Math.Min(totalCount, _options.MaxPdfRows));
        await foreach (var row in dataSource.FetchRowsAsync(filter, _options.ChunkSize, cancellationToken))
        {
            rows.Add(row);
        }

        var columns = dataSource.Columns;

        // Calculate column totals for Table Footer
        var columnTotals = new decimal[columns.Count];
        foreach (var rowData in rows)
        {
            for (var colIdx = 0; colIdx < rowData.Count && colIdx < columns.Count; colIdx++)
            {
                if (columns[colIdx].Kind == ReportColumnKind.Decimal &&
                    rowData[colIdx] != null &&
                    decimal.TryParse(rowData[colIdx]!.ToString(), out var decVal))
                {
                    columnTotals[colIdx] += decVal;
                }
            }
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                // Page Header
                page.Header().Element(headerContainer =>
                {
                    headerContainer.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(dataSource.Title).FontSize(16).Bold().FontColor("#1A365D");
                            col.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | Records: {rows.Count:N0}")
                                .FontSize(8).FontColor("#718096");
                        });
                    });
                });

                // Page Content
                page.Content().PaddingTop(8).Column(contentCol =>
                {
                    // 1. Summary Totals Section (Outside Table)
                    if (summary != null)
                    {
                        contentCol.Item().PaddingBottom(12).Row(kpiRow =>
                        {
                            kpiRow.Spacing(8);

                            // Card 1: First Previous Balance
                            kpiRow.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(7).Column(c =>
                            {
                                c.Item().Text("First Previous Balance").FontSize(7.5f).FontColor("#64748B");
                                c.Item().Text(summary.FirstPreviousBalance.ToString("#,##0.00")).FontSize(11).Bold().FontColor("#1E293B");
                            });

                            // Card 2: Total Debits (account)
                            kpiRow.RelativeItem().Border(1).BorderColor("#A7F3D0").Background("#ECFDF5").Padding(7).Column(c =>
                            {
                                c.Item().Text("Total Debits (account)").FontSize(7.5f).FontColor("#047857");
                                c.Item().Text(summary.TotalDebits.ToString("#,##0.00")).FontSize(11).Bold().FontColor("#065F46");
                            });

                            // Card 3: Total Credits (account)
                            kpiRow.RelativeItem().Border(1).BorderColor("#FECDD3").Background("#FFF1F2").Padding(7).Column(c =>
                            {
                                c.Item().Text("Total Credits (account)").FontSize(7.5f).FontColor("#BE123C");
                                c.Item().Text(summary.TotalCredits.ToString("#,##0.00")).FontSize(11).Bold().FontColor("#9F1239");
                            });

                            // Card 4: Final Balance (account)
                            kpiRow.RelativeItem().Border(1).BorderColor("#BAE6FD").Background("#F0F9FF").Padding(7).Column(c =>
                            {
                                c.Item().Text("Final Balance (account)").FontSize(7.5f).FontColor("#0369A1");
                                c.Item().Text(summary.FinalBalance.ToString("#,##0.00")).FontSize(11).Bold().FontColor("#075985");
                            });
                        });
                    }

                    // 2. Table
                    contentCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(def =>
                        {
                            foreach (var col in columns)
                            {
                                def.RelativeColumn(col.RelativeWidth);
                            }
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            foreach (var col in columns)
                            {
                                header.Cell()
                                    .Background("#2980B9")
                                    .Padding(5)
                                    .BorderBottom(1)
                                    .BorderColor("#BDC3C7")
                                    .AlignMiddle()
                                    .Element(c => col.Kind is ReportColumnKind.Integer or ReportColumnKind.Decimal
                                        ? c.AlignRight()
                                        : c.AlignLeft())
                                    .Text(col.Header)
                                    .FontColor(Colors.White)
                                    .Bold();
                            }
                        });

                        // Table Rows
                        for (var i = 0; i < rows.Count; i++)
                        {
                            var rowData = rows[i];
                            var background = i % 2 == 0 ? "#F8F9FA" : "#FFFFFF";

                            for (var colIdx = 0; colIdx < rowData.Count && colIdx < columns.Count; colIdx++)
                            {
                                var val = rowData[colIdx];
                                var colMeta = columns[colIdx];
                                var formattedText = FormatValue(val, colMeta.Kind);

                                table.Cell()
                                    .Background(background)
                                    .Padding(4)
                                    .BorderBottom(0.5f)
                                    .BorderColor("#E2E8F0")
                                    .AlignMiddle()
                                    .Element(c => colMeta.Kind is ReportColumnKind.Integer or ReportColumnKind.Decimal
                                        ? c.AlignRight()
                                        : c.AlignLeft())
                                    .Text(formattedText);
                            }
                        }

                        // 3. Table Footer (Totals row)
                        if (rows.Count > 0)
                        {
                            table.Footer(footer =>
                            {
                                for (var colIdx = 0; colIdx < columns.Count; colIdx++)
                                {
                                    var colMeta = columns[colIdx];
                                    var cell = footer.Cell()
                                        .Background("#EAEDED")
                                        .Padding(5)
                                        .BorderTop(1)
                                        .BorderColor("#94A3B8")
                                        .BorderBottom(1.5f)
                                        .BorderColor("#2C3E50")
                                        .AlignMiddle();

                                    if (colIdx == 0)
                                    {
                                        cell.AlignLeft().Text("Total").Bold().FontColor("#1A365D");
                                    }
                                    else if (colMeta.Kind == ReportColumnKind.Decimal)
                                    {
                                        cell.AlignRight().Text(columnTotals[colIdx].ToString("#,##0.00")).Bold().FontColor("#1A365D");
                                    }
                                    else
                                    {
                                        cell.Text(string.Empty);
                                    }
                                }
                            });
                        }
                    });
                });

                // Page Footer
                page.Footer().Element(footerContainer =>
                {
                    footerContainer.Row(row =>
                    {
                        row.RelativeItem().Text("EBSS Reporting Service").FontSize(8).FontColor("#A0AEC0");
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                });
            });
        });

        document.GeneratePdf(outputStream);
    }

    private static string FormatValue(object? value, ReportColumnKind kind)
    {
        if (value == null) return string.Empty;

        return kind switch
        {
            ReportColumnKind.Integer when long.TryParse(value.ToString(), out var intVal) => intVal.ToString("#,##0"),
            ReportColumnKind.Decimal when decimal.TryParse(value.ToString(), out var decVal) => decVal.ToString("#,##0.00"),
            ReportColumnKind.DateTime when DateTime.TryParse(value.ToString(), out var dtVal) => dtVal.ToString("yyyy-MM-dd HH:mm"),
            _ => value.ToString() ?? string.Empty
        };
    }
}
