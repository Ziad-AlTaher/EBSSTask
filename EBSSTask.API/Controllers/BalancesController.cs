using EBSSTask.Application.Common;
using EBSSTask.Application.DTOs.Balance;
using EBSSTask.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EBSSTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalancesController : ControllerBase
{
    private readonly IBalanceService _balanceService;

    public BalancesController(IBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    /// <summary>
    /// Gets a paginated list of Balances with optional search query.
    /// </summary>
    /// <param name="parameters">Pagination and search parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of balances</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BalanceDto>>> GetBalances([FromQuery] BalanceQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _balanceService.GetBalancesAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single Balance by its ID.
    /// </summary>
    /// <param name="id">The Balance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Balance details if found</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var balance = await _balanceService.GetBalanceByIdAsync(id, cancellationToken);

        if (balance == null)
        {
            return NotFound(new { message = $"Balance with ID {id} not found." });
        }

        return Ok(balance);
    }
}
