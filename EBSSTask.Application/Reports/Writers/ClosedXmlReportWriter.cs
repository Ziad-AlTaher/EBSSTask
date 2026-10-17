using ClosedXML.Excel;
using EBSSTask.Application.Common.Options;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EBSSTask.Application.Reports.Writers;

public sealed class ClosedXmlReportWriter : IReportWriter
{
    private readonly ReportGenerationOptions _options;

    public ClosedXmlReportWriter(IOptions<ReportGenerationOptions> options)
    {
        _options = options.Value;
    }

    public ReportFormat Format => ReportFormat.Excel;

    public async Task WriteAsync(
        IReportDataSource dataSource,
        ReportFilter filter,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Report");

        // Fetch summary (via Stored Procedure for BalanceHistory)
        var summary = await dataSource.GetSummaryAsync(filter, cancellationToken);

        var startRow = 1;
        var columns = dataSource.Columns;

        // 1. Summary Totals Section (Outside Table)
        if (summary != null)
        {
            // Title & Metadata
            worksheet.Cell(1, 1).Value = dataSource.Title;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(26, 54, 93);

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | Total Records: {summary.TotalRecords:N0}";
            worksheet.Cell(2, 1).Style.Font.FontSize = 9;
            worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(113, 128, 150);

            // 4 KPI Summary Cards (Rows 4-5)
            RenderSummaryCard(worksheet, 4, 1, 2, "First Previous Balance", summary.FirstPreviousBalance, XLColor.FromArgb(241, 245, 249), XLColor.FromArgb(44, 62, 80));
            RenderSummaryCard(worksheet, 4, 3, 3, "Total Debits (account)", summary.TotalDebits, XLColor.FromArgb(236, 253, 245), XLColor.FromArgb(39, 174, 96));
            RenderSummaryCard(worksheet, 4, 4, 4, "Total Credits (account)", summary.TotalCredits, XLColor.FromArgb(254, 242, 242), XLColor.FromArgb(192, 57, 43));
            RenderSummaryCard(worksheet, 4, 5, 6, "Final Balance (account)", summary.FinalBalance, XLColor.FromArgb(239, 246, 255), XLColor.FromArgb(41, 128, 185));

            startRow = 7;
        }

        // 2. Table Headers
        for (var colIndex = 0; colIndex < columns.Count; colIndex++)
        {
            var cell = worksheet.Cell(startRow, colIndex + 1);
            cell.Value = columns[colIndex].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(41, 128, 185);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(189, 195, 199);
        }

        // 3. Data Rows
        var dataStartRow = startRow + 1;
        var rowIndex = dataStartRow;

        await foreach (var row in dataSource.FetchRowsAsync(filter, _options.ChunkSize, cancellationToken))
        {
            for (var colIndex = 0; colIndex < row.Count && colIndex < columns.Count; colIndex++)
            {
                var value = row[colIndex];
                var cell = worksheet.Cell(rowIndex, colIndex + 1);
                var columnMeta = columns[colIndex];

                if (value == null)
                {
                    cell.Value = string.Empty;
                }
                else
                {
                    switch (columnMeta.Kind)
                    {
                        case ReportColumnKind.Integer when long.TryParse(value.ToString(), out var intVal):
                            cell.Value = intVal;
                            cell.Style.NumberFormat.Format = "#,##0";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;

                        case ReportColumnKind.Decimal when decimal.TryParse(value.ToString(), out var decVal):
                            cell.Value = decVal;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;

                        case ReportColumnKind.DateTime when DateTime.TryParse(value.ToString(), out var dtVal):
                            cell.Value = dtVal;
                            cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            break;

                        default:
                            cell.Value = value.ToString() ?? string.Empty;
                            break;
                    }
                }

                if ((rowIndex - dataStartRow) % 2 == 1)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                }

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(220, 224, 230);
            }

            rowIndex++;
        }

        var dataEndRow = rowIndex - 1;

        // 4. Table Footer (Totals row)
        if (dataEndRow >= dataStartRow)
        {
            var footerRow = rowIndex;
            for (var colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var cell = worksheet.Cell(footerRow, colIndex + 1);
                var columnMeta = columns[colIndex];
                var colLetter = GetExcelColumnName(colIndex + 1);

                if (colIndex == 0)
                {
                    cell.Value = "Total";
                    cell.Style.Font.Bold = true;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (columnMeta.Kind == ReportColumnKind.Decimal)
                {
                    cell.FormulaA1 = $"=SUM({colLetter}{dataStartRow}:{colLetter}{dataEndRow})";
                    cell.Style.Font.Bold = true;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(234, 237, 237);
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.TopBorderColor = XLColor.FromArgb(149, 165, 166);
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                cell.Style.Border.BottomBorderColor = XLColor.FromArgb(44, 62, 80);
            }
        }

        // Auto-fit columns with safety limits
        worksheet.Columns(1, columns.Count).AdjustToContents(startRow, Math.Min(rowIndex, 100));

        workbook.SaveAs(outputStream);
    }

    private static void RenderSummaryCard(
        IXLWorksheet worksheet,
        int row,
        int startCol,
        int endCol,
        string title,
        decimal value,
        XLColor bg,
        XLColor textCol)
    {
        var titleRange = worksheet.Range(row, startCol, row, endCol);
        if (startCol != endCol) titleRange.Merge();
        titleRange.Value = title;
        titleRange.Style.Font.FontSize = 8;
        titleRange.Style.Font.FontColor = XLColor.FromArgb(100, 116, 139);
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Fill.BackgroundColor = bg;
        titleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        titleRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(203, 213, 225);

        var valRange = worksheet.Range(row + 1, startCol, row + 1, endCol);
        if (startCol != endCol) valRange.Merge();
        valRange.Value = value;
        valRange.Style.Font.Bold = true;
        valRange.Style.Font.FontSize = 11;
        valRange.Style.Font.FontColor = textCol;
        valRange.Style.NumberFormat.Format = "#,##0.00";
        valRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        valRange.Style.Fill.BackgroundColor = bg;
        valRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        valRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(203, 213, 225);
    }

    private static string GetExcelColumnName(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }
}
