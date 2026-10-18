using EBSSTask.Application.Common;
using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Common.Queries;
using EBSSTask.Application.DTOs.BalanceHistory;
using EBSSTask.Application.Interfaces;
using EBSSTask.Application.Reports.Models;
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
        var query = _context.BalanceHistories
            .AsNoTracking()
            .ApplyFilter(parameters.BalanceId, parameters.FromDate, parameters.ToDate)
            .ApplySearch(parameters.Search);

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

    public async Task<ReportSummaryData> GetSummaryAsync(
        int? balanceId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.GetBalanceHistorySummaryAsync(balanceId, fromDate, toDate, cancellationToken);
    }
}
