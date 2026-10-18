export interface BalanceHistoryDto {
  balanceHisId: number;
  requestNo?: number;
  balanceId?: number;
  kind?: string;
  debtor?: number;
  creditor?: number;
  prevBalnce?: number;
  newBalnce?: number;
  orderBill?: string;
  date?: string;
  remarks?: string;
  billArrange?: number;
  insertDate?: string;
  este7kakDate?: string;
  orgId?: number;
  costCenterId?: number;
  branchId?: number;
  referenceNo?: string;

  // Computed for table display
  calculatedFinalBalance?: number;
}

export interface BalanceHistoryQueryParameters {
  balanceId?: number;
  fromDate?: string;
  toDate?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}

/**
 * Lightweight projected model storing ONLY required display fields
 * to minimize client-side memory footprint in V8 heap during infinite scroll.
 */
export interface ProjectedBalanceHistoryRow {
  id: number;
  date?: string;
  orderBill?: string;
  kind?: string;
  prev: number;
  debit: number;
  credit: number;
  final: number;
}

export function projectBalanceHistoryRow(
  dto: BalanceHistoryDto,
  isCreditAccount: boolean
): ProjectedBalanceHistoryRow {
  const prev = Number(dto.prevBalnce) || 0;
  const debit = Number(dto.debtor) || 0;
  const credit = Number(dto.creditor) || 0;
  const calculatedFinal = isCreditAccount
    ? (prev + credit) - debit
    : (prev + debit) - credit;

  return {
    id: dto.balanceHisId,
    date: dto.date,
    orderBill: dto.orderBill,
    kind: dto.kind,
    prev,
    debit,
    credit,
    final: calculatedFinal
  };
}



