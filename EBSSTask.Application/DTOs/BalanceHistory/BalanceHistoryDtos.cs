namespace EBSSTask.Application.DTOs.BalanceHistory;

public class BalanceHistoryDto
{
    public long BalanceHisId { get; set; }
    public int? RequestNo { get; set; }
    public int? BalanceId { get; set; }
    public string? Kind { get; set; }
    public decimal? Debtor { get; set; }
    public decimal? Creditor { get; set; }
    public decimal? PrevBalnce { get; set; }
    public decimal? NewBalnce { get; set; }
    public string? OrderBill { get; set; }
    public DateTime? Date { get; set; }
    public string? Remarks { get; set; }
    public int? BillArrange { get; set; }
    public DateTime? InsertDate { get; set; }
    public DateTime? Este7kakDate { get; set; }
    public int? OrgId { get; set; }
    public int? CostCenterId { get; set; }
    public int? BranchId { get; set; }
    public string? ReferenceNo { get; set; }
}

public class BalanceHistoryQueryParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int? BalanceId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
