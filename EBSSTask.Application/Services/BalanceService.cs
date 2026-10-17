using EBSSTask.Application.Common;
using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Common.Queries;
using EBSSTask.Application.DTOs.Balance;
using EBSSTask.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBSSTask.Application.Services;

public class BalanceService : IBalanceService
{
    private readonly IApplicationDbContext _context;

    public BalanceService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BalanceDto>> GetBalancesAsync(BalanceQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _context.Balances
            .AsNoTracking()
            .ApplySearch(parameters.Search);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(b => b.BalanceName)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(b => new BalanceDto
            {
                BalanceId = b.BalanceId,
                BalanceName = b.BalanceName,
                BalanceNameEn = b.BalanceNameEn,
                BalanceType = b.BalanceType,
                ParentId = b.ParentId,
                ParentCode = b.ParentCode,
                Sub1Id = b.Sub1Id,
                Sub2Id = b.Sub2Id,
                Sub3Id = b.Sub3Id,
                Sub4Id = b.Sub4Id,
                Sub5Id = b.Sub5Id,
                Sub6Id = b.Sub6Id,
                Sub7Id = b.Sub7Id,
                OprType = b.OprType,
                OprMD = b.OprMD,
                OprKindD = b.OprKindD,
                Class = b.Class,
                AllowPointSystem = b.AllowPointSystem,
                OrgId = b.OrgId,
                Fixed = b.Fixed,
                BLevel = b.BLevel
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BalanceDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<BalanceDto?> GetBalanceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Balances
            .AsNoTracking()
            .Where(b => b.BalanceId == id)
            .Select(b => new BalanceDto
            {
                BalanceId = b.BalanceId,
                BalanceName = b.BalanceName,
                BalanceNameEn = b.BalanceNameEn,
                BalanceType = b.BalanceType,
                ParentId = b.ParentId,
                ParentCode = b.ParentCode,
                Sub1Id = b.Sub1Id,
                Sub2Id = b.Sub2Id,
                Sub3Id = b.Sub3Id,
                Sub4Id = b.Sub4Id,
                Sub5Id = b.Sub5Id,
                Sub6Id = b.Sub6Id,
                Sub7Id = b.Sub7Id,
                OprType = b.OprType,
                OprMD = b.OprMD,
                OprKindD = b.OprKindD,
                Class = b.Class,
                AllowPointSystem = b.AllowPointSystem,
                OrgId = b.OrgId,
                Fixed = b.Fixed,
                BLevel = b.BLevel
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
