using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EBSSTask.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Balance> Balances { get; }
    DbSet<BalanceHistory> BalanceHistories { get; }
    DbSet<FileProcess> FileProcesses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ReportSummaryData> GetBalanceHistorySummaryAsync(
        int? balanceId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
}
