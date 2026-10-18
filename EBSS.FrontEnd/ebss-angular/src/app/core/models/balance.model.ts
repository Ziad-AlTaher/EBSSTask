export interface BalanceDto {
  balanceId: number;
  balanceName?: string;
  balanceNameEn?: string;
  balanceType?: string; // 'D' = Debit, 'C' = Credit
  parentId?: number;
  parentCode?: number;
  sub1Id?: number;
  sub2Id?: number;
  sub3Id?: number;
  sub4Id?: number;
  sub5Id?: number;
  sub6Id?: number;
  sub7Id?: number;
  oprType?: string;
  oprMD?: string;
  oprKindD?: string;
  class?: number;
  allowPointSystem?: string;
  orgId?: number;
  fixed?: string;
  bLevel?: string;
}

export interface BalanceQueryParameters {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}
