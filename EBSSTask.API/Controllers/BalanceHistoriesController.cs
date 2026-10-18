using EBSSTask.Application.Common;
using EBSSTask.Application.DTOs.BalanceHistory;
using EBSSTask.Application.Interfaces;
using EBSSTask.Application.Reports.Models;
using Microsoft.AspNetCore.Mvc;

namespace EBSSTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalanceHistoriesController : ControllerBase
{
    private readonly IBalanceHistoryService _balanceHistoryService;
    private readonly IDirectExportService _directExportService;

    public BalanceHistoriesController(
        IBalanceHistoryService balanceHistoryService,
        IDirectExportService directExportService)
    {
        _balanceHistoryService = balanceHistoryService;
        _directExportService = directExportService;
    }

    /// <summary>
    /// Gets a paginated list of Balance Histories with optional balance and date filters.
    /// </summary>
    /// <param name="parameters">Pagination and filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of balance histories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BalanceHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BalanceHistoryDto>>> GetBalanceHistories([FromQuery] BalanceHistoryQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _balanceHistoryService.GetBalanceHistoriesAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single Balance History by its ID.
    /// </summary>
    /// <param name="id">The Balance History ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Balance History details if found</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(BalanceHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceHistoryDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var history = await _balanceHistoryService.GetBalanceHistoryByIdAsync(id, cancellationToken);

        if (history == null)
        {
            return NotFound(new { message = $"Balance History with ID {id} not found." });
        }

        return Ok(history);
    }

    /// <summary>
    /// Gets summary calculations via Stored Procedure dbo.sp_GetBalanceHistorySummary.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ReportSummaryData), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportSummaryData>> GetSummary(
        [FromQuery] int? balanceId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var summary = await _balanceHistoryService.GetSummaryAsync(balanceId, fromDate, toDate, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Direct in-memory synchronous export of the current grid page to PDF.
    /// Uses the exact same query parameters as GetBalanceHistories.
    /// </summary>
    [HttpGet("export-direct/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDirectPdf([FromQuery] BalanceHistoryQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _balanceHistoryService.GetBalanceHistoriesAsync(parameters, cancellationToken);
        var fileBytes = _directExportService.GenerateDirectPdf(result.Items, parameters);
        var fileName = $"Statement_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        return File(fileBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Direct in-memory synchronous export of the current grid page to Excel.
    /// Uses the exact same query parameters as GetBalanceHistories.
    /// </summary>
    [HttpGet("export-direct/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDirectExcel([FromQuery] BalanceHistoryQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _balanceHistoryService.GetBalanceHistoriesAsync(parameters, cancellationToken);
        var fileBytes = _directExportService.GenerateDirectExcel(result.Items, parameters);
        var fileName = $"Statement_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
