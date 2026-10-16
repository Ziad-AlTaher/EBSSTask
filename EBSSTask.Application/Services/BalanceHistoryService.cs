using EBSSTask.Application.Common;
using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.DTOs.BalanceHistory;
using EBSSTask.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBSSTask.Application.Services;

public class BalanceHistoryService : IBalanceHistoryService
{
    private readonly IApplicationDbContext _context;

    public BalanceHistoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BalanceHistoryDto>> GetBalanceHistoriesAsync(BalanceHistoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _context.BalanceHistories.AsNoTracking().AsQueryable();

        // Filter by specific BalanceId if provided
        if (parameters.BalanceId.HasValue)
        {
            query = query.Where(h => h.BalanceId == parameters.BalanceId.Value);
        }

        // Filter by date range if provided
        if (parameters.FromDate.HasValue)
        {
            query = query.Where(h => h.Date >= parameters.FromDate.Value);
        }

        if (parameters.ToDate.HasValue)
        {
            query = query.Where(h => h.Date <= parameters.ToDate.Value);
        }

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();

            query = query.Where(h =>
                (h.Remarks != null && h.Remarks.Contains(search)) ||
                (h.OrderBill != null && h.OrderBill.Contains(search)) ||
                (h.ReferenceNo != null && h.ReferenceNo.Contains(search)) ||
                (h.Kind != null && h.Kind.Contains(search)) ||
                (h.RequestNo.HasValue && h.RequestNo.Value.ToString().Contains(search)) ||
                (h.BalanceId.HasValue && h.BalanceId.Value.ToString().Contains(search)) ||
                h.BalanceHisId.ToString() == search
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(h => h.BalanceHisId)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(h => new BalanceHistoryDto
            {
                BalanceHisId = h.BalanceHisId,
                RequestNo = h.RequestNo,
                BalanceId = h.BalanceId,
                Kind = h.Kind,
                Debtor = h.Debtor,
                Creditor = h.Creditor,
                PrevBalnce = h.PrevBalnce,
                NewBalnce = h.NewBalnce,
                OrderBill = h.OrderBill,
                Date = h.Date,
                Remarks = h.Remarks,
                BillArrange = h.BillArrange,
                InsertDate = h.InsertDate,
                Este7kakDate = h.Este7kakDate,
                OrgId = h.OrgId,
                CostCenterId = h.CostCenterId,
                BranchId = h.BranchId,
                ReferenceNo = h.ReferenceNo
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BalanceHistoryDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<BalanceHistoryDto?> GetBalanceHistoryByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.BalanceHistories
            .AsNoTracking()
            .Where(h => h.BalanceHisId == id)
            .Select(h => new BalanceHistoryDto
            {
                BalanceHisId = h.BalanceHisId,
                RequestNo = h.RequestNo,
                BalanceId = h.BalanceId,
                Kind = h.Kind,
                Debtor = h.Debtor,
                Creditor = h.Creditor,
                PrevBalnce = h.PrevBalnce,
                NewBalnce = h.NewBalnce,
                OrderBill = h.OrderBill,
                Date = h.Date,
                Remarks = h.Remarks,
                BillArrange = h.BillArrange,
                InsertDate = h.InsertDate,
                Este7kakDate = h.Este7kakDate,
                OrgId = h.OrgId,
                CostCenterId = h.CostCenterId,
                BranchId = h.BranchId,
                ReferenceNo = h.ReferenceNo
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
