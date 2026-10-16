using EBSSTask.Application.Common;
using EBSSTask.Application.DTOs.Balance;

namespace EBSSTask.Application.Interfaces;

public interface IBalanceService
{
    Task<PagedResult<BalanceDto>> GetBalancesAsync(BalanceQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<BalanceDto?> GetBalanceByIdAsync(int id, CancellationToken cancellationToken = default);
}
