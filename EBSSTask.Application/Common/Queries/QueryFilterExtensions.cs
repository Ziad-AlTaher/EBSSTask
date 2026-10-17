using EBSSTask.Domain.Entities;

namespace EBSSTask.Application.Common.Queries;

/// <summary>
/// Shared filtering rules so the paginated APIs and the file exports always return the same data.
/// </summary>
public static class QueryFilterExtensions
{
    public static IQueryable<Balance> ApplySearch(this IQueryable<Balance> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim();

        return query.Where(b =>
            (b.BalanceName != null && b.BalanceName.Contains(term)) ||
            (b.BalanceNameEn != null && b.BalanceNameEn.Contains(term)) ||
            b.BalanceId.ToString() == term);
    }

    public static IQueryable<BalanceHistory> ApplyFilter(
        this IQueryable<BalanceHistory> query,
        int? balanceId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (balanceId.HasValue)
        {
            query = query.Where(h => h.BalanceId == balanceId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.Date <= toDate.Value);
        }

        return query;
    }
}
