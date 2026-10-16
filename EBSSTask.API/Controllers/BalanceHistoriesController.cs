using EBSSTask.Application.Common;
using EBSSTask.Application.DTOs.BalanceHistory;
using EBSSTask.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EBSSTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalanceHistoriesController : ControllerBase
{
    private readonly IBalanceHistoryService _balanceHistoryService;

    public BalanceHistoriesController(IBalanceHistoryService balanceHistoryService)
    {
        _balanceHistoryService = balanceHistoryService;
    }

    /// <summary>
    /// Gets a paginated list of Balance Histories with optional search, balance, and date filters.
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
}
