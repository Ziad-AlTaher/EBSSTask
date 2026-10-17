using EBSSTask.Application.Common;
using EBSSTask.Application.Reports;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EBSSTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportStorage _reportStorage;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IReportService reportService,
        IReportStorage reportStorage,
        ILogger<FilesController> logger)
    {
        _reportService = reportService;
        _reportStorage = reportStorage;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of all created report files and their statuses.
    /// </summary>
    /// <param name="parameters">Pagination and optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FileProcessStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FileProcessStatusDto>>> GetAllFiles(
        [FromQuery] FileProcessQueryParameters? parameters,
        CancellationToken cancellationToken)
    {
        parameters ??= new FileProcessQueryParameters();
        var result = await _reportService.GetAllFilesAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the processing status of a report generation task.
    /// </summary>
    /// <param name="processId">The unique process ID returned when requested.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{processId:guid}")]
    [ProducesResponseType(typeof(FileProcessStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileProcessStatusDto>> GetStatus(
        Guid processId,
        CancellationToken cancellationToken)
    {
        var status = await _reportService.GetStatusAsync(processId, cancellationToken);
        if (status == null)
        {
            return NotFound(new { message = $"Process with ID {processId} was not found." });
        }

        return Ok(status);
    }

    /// <summary>
    /// Downloads the generated report file once processing is complete.
    /// </summary>
    /// <param name="processId">The unique process ID.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{processId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadFile(
        Guid processId,
        CancellationToken cancellationToken)
    {
        var process = await _reportService.GetProcessEntityAsync(processId, cancellationToken);
        if (process == null)
        {
            return NotFound(new { message = $"Process with ID {processId} was not found." });
        }

        switch (process.ProcessStatus)
        {
            case FileProcessStatus.Pending:
            case FileProcessStatus.Processing:
                return Conflict(new
                {
                    status = process.ProcessStatus.ToString(),
                    message = "File is still being generated. Please retry shortly.",
                    statusUrl = ReportRoutes.Status(processId)
                });

            case FileProcessStatus.Failed:
                return BadRequest(new
                {
                    status = FileProcessStatus.Failed.ToString(),
                    message = "File generation failed.",
                    error = process.ErrorMessage
                });

            case FileProcessStatus.Completed:
                if (string.IsNullOrWhiteSpace(process.FilePath))
                {
                    _logger.LogError("Process {ProcessId} is marked Completed but FilePath is null or empty.", processId);
                    return NotFound(new { message = "Generated file path record is missing." });
                }

                var stream = await _reportStorage.OpenReadAsync(process.FilePath, cancellationToken);
                if (stream == null)
                {
                    _logger.LogWarning("File for process {ProcessId} at {FilePath} not found on disk.", processId, process.FilePath);
                    return NotFound(new { message = "The generated file was not found on the server." });
                }

                var contentType = process.ReportFormat.GetContentType();
                var fileName = Path.GetFileName(process.FilePath);

                return File(stream, contentType, fileName, enableRangeProcessing: true);

            default:
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unknown process status." });
        }
    }
}
