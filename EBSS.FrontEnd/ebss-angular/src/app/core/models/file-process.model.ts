export enum FileProcessStatus {
  Pending = 1,
  Processing = 2,
  Completed = 3,
  Failed = 4
}

export enum ReportType {
  Balances = 1,
  BalanceHistory = 2
}

export enum ReportFormat {
  Excel = 1,
  Pdf = 2
}

export interface FileProcessStatusDto {
  id: string;
  reportType: ReportType | string | number;
  reportFormat: ReportFormat | string | number;
  processStatus: FileProcessStatus | string | number;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  downloadUrl?: string;
  errorMessage?: string;
}

export function getFileProcessStatusText(status: FileProcessStatus | string | number): string {
  if (status === FileProcessStatus.Pending || status === 'Pending' || status === 1) return 'Pending';
  if (status === FileProcessStatus.Processing || status === 'Processing' || status === 2) return 'Processing';
  if (status === FileProcessStatus.Completed || status === 'Completed' || status === 3) return 'Completed';
  if (status === FileProcessStatus.Failed || status === 'Failed' || status === 4) return 'Failed';
  return String(status);
}

export function getReportTypeText(type: ReportType | string | number): string {
  if (type === ReportType.BalanceHistory || type === 'BalanceHistory' || type === 2) return 'Balance History';
  if (type === ReportType.Balances || type === 'Balances' || type === 1) return 'Balances';
  return String(type);
}

export function getReportFormatText(format: ReportFormat | string | number): string {
  if (format === ReportFormat.Excel || format === 'Excel' || format === 1) return 'Excel (.xlsx)';
  if (format === ReportFormat.Pdf || format === 'Pdf' || format === 2) return 'PDF (QuestPDF)';
  return String(format);
}
