using System.Runtime.CompilerServices;
using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Common.Queries;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EBSSTask.Application.Reports.DataSources;

public sealed class BalanceHistoryReportDataSource : IReportDataSource
{
    private readonly IApplicationDbContext _context;

    public BalanceHistoryReportDataSource(IApplicationDbContext context)
    {
        _context = context;
    }

    public ReportType Type => ReportType.BalanceHistory;

    public string Title => "Balance History Report";

    public IReadOnlyList<ReportColumn> Columns { get; } =
    [
        new("Account ID", ReportColumnKind.Integer, 1.0f),
        new("Account Name", ReportColumnKind.Text, 2.5f),
        new("Previous Balance", ReportColumnKind.Decimal, 1.3f),
        new("Debit Amount", ReportColumnKind.Decimal, 1.3f),
        new("Credit Amount", ReportColumnKind.Decimal, 1.3f),
        new("Final Balance", ReportColumnKind.Decimal, 1.3f)
    ];

    public async Task<int> GetTotalCountAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        return await _context.BalanceHistories
            .AsNoTracking()
            .ApplyFilter(filter.BalanceId, filter.FromDate, filter.ToDate)
            .CountAsync(cancellationToken);
    }

    public async IAsyncEnumerable<IReadOnlyList<object?>> FetchRowsAsync(
        ReportFilter filter,
        int chunkSize,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var total = await GetTotalCountAsync(filter, cancellationToken);
        var page = 0;

        while (page * chunkSize < total)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkQuery = from h in _context.BalanceHistories.AsNoTracking()
                                 .ApplyFilter(filter.BalanceId, filter.FromDate, filter.ToDate)
                                 .OrderBy(h => h.BalanceHisId)
                                 .Skip(page * chunkSize)
                                 .Take(chunkSize)
                             join b in _context.Balances.AsNoTracking()
                                 on h.BalanceId equals b.BalanceId into bGroup
                             from b in bGroup.DefaultIfEmpty()
                             select new
                             {
                                 AccountId = h.BalanceId,
                                 AccountName = b != null ? (b.BalanceName ?? string.Empty) : string.Empty,
                                 PreviousBalance = h.PrevBalnce ?? 0m,
                                 DebitAmount = h.Debtor ?? 0m,
                                 CreditAmount = h.Creditor ?? 0m,
                                 AccountType = b != null ? b.BalanceType : "D"
                             };

            var chunk = await chunkQuery.ToListAsync(cancellationToken);

            foreach (var item in chunk)
            {
                var isCredit = string.Equals(item.AccountType?.Trim(), "C", StringComparison.OrdinalIgnoreCase);
                var finalBalance = isCredit
                    ? (item.PreviousBalance + item.CreditAmount) - item.DebitAmount
                    : (item.PreviousBalance + item.DebitAmount) - item.CreditAmount;

                yield return new object?[]
                {
                    item.AccountId,
                    item.AccountName,
                    item.PreviousBalance,
                    item.DebitAmount,
                    item.CreditAmount,
                    finalBalance
                };
            }

            page++;
        }
    }

    public async Task<ReportSummaryData?> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        return await _context.GetBalanceHistorySummaryAsync(
            filter.BalanceId,
            filter.FromDate,
            filter.ToDate,
            cancellationToken);
    }
}
