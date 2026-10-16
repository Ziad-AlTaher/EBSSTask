using EBSSTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EBSSTask.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Balance> Balances { get; }
    DbSet<BalanceHistory> BalanceHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
