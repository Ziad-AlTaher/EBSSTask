using System.ComponentModel.DataAnnotations;

namespace EBSSTask.Application.DTOs.Balance;

public class BalanceDto
{
    public int BalanceId { get; set; }
    public string? BalanceName { get; set; }
    public string? BalanceNameEn { get; set; }
    public string? BalanceType { get; set; }
    public int? ParentId { get; set; }
    public int? ParentCode { get; set; }
    public int? Sub1Id { get; set; }
    public int? Sub2Id { get; set; }
    public int? Sub3Id { get; set; }
    public int? Sub4Id { get; set; }
    public int? Sub5Id { get; set; }
    public int? Sub6Id { get; set; }
    public int? Sub7Id { get; set; }
    public string? OprType { get; set; }
    public string? OprMD { get; set; }
    public string? OprKindD { get; set; }
    public int? Class { get; set; }
    public string? AllowPointSystem { get; set; }
    public int? OrgId { get; set; }
    public string? Fixed { get; set; }
    public string? BLevel { get; set; }
}



public class BalanceQueryParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}
