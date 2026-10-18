export interface ReportRequestParameters {
  balanceId?: number;
  fromDate?: string;
  toDate?: string;
  connectionId?: string;
}

export interface ReportSubmissionResult {
  processId: string;
  statusUrl: string;
  downloadUrl: string;
  message: string;
}

export interface ReportSummaryData {
  firstPreviousBalance: number;
  totalDebits: number;
  totalCredits: number;
  finalBalance: number;
  totalPreviousBalances: number;
  totalFinalBalances: number;
  totalRecords: number;
  accountType?: string;
}
