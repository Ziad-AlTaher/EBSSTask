using EBSSTask.Application.Common;
using EBSSTask.Application.DTOs.BalanceHistory;

namespace EBSSTask.Application.Interfaces;

public interface IBalanceHistoryService
{
    Task<PagedResult<BalanceHistoryDto>> GetBalanceHistoriesAsync(BalanceHistoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<BalanceHistoryDto?> GetBalanceHistoryByIdAsync(long id, CancellationToken cancellationToken = default);
}
