using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EBSSTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Request a ClosedXML Excel report for BalanceHistory.
    /// Returns 202 Accepted immediately with ProcessId and tracking URLs.
    /// </summary>
    [HttpPost("balance-history/excel")]
    [ProducesResponseType(typeof(ReportSubmissionResult), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestBalanceHistoryExcel(
        [FromBody] ReportRequestParameters? parameters,
        CancellationToken cancellationToken)
    {
        parameters ??= new ReportRequestParameters();

        var request = new GenerateReportRequest(
            ReportType: ReportType.BalanceHistory,
            ReportFormat: ReportFormat.Excel,
            Filter: parameters.ToFilter(),
            ConnectionId: parameters.ConnectionId);

        var result = await _reportService.RequestReportAsync(request, cancellationToken);
        return Accepted(result.StatusUrl, result);
    }

    /// <summary>
    /// Request a QuestPDF document for BalanceHistory.
    /// Returns 202 Accepted immediately with ProcessId and tracking URLs.
    /// </summary>
    [HttpPost("balance-history/pdf")]
    [ProducesResponseType(typeof(ReportSubmissionResult), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestBalanceHistoryPdf(
        [FromBody] ReportRequestParameters? parameters,
        CancellationToken cancellationToken)
    {
        parameters ??= new ReportRequestParameters();

        var request = new GenerateReportRequest(
            ReportType: ReportType.BalanceHistory,
            ReportFormat: ReportFormat.Pdf,
            Filter: parameters.ToFilter(),
            ConnectionId: parameters.ConnectionId);

        var result = await _reportService.RequestReportAsync(request, cancellationToken);
        return Accepted(result.StatusUrl, result);
    }

}

public class ReportRequestParameters
{
    public int? BalanceId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ConnectionId { get; set; }

    public ReportFilter ToFilter() => new()
    {
        BalanceId = BalanceId,
        FromDate = FromDate,
        ToDate = ToDate
    };
}
