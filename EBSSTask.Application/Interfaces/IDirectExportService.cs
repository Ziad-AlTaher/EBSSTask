using EBSSTask.Application.DTOs.BalanceHistory;

namespace EBSSTask.Application.Interfaces;

public interface IDirectExportService
{
    byte[] GenerateDirectPdf(IEnumerable<BalanceHistoryDto> items, BalanceHistoryQueryParameters parameters);
    byte[] GenerateDirectExcel(IEnumerable<BalanceHistoryDto> items, BalanceHistoryQueryParameters parameters);
}


