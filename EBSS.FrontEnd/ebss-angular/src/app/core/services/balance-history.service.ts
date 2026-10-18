import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiConfigService } from './api-config.service';
import { BalanceHistoryDto, BalanceHistoryQueryParameters } from '../models/balance-history.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({
  providedIn: 'root'
})
export class BalanceHistoryService {
  private http = inject(HttpClient);
  private config = inject(ApiConfigService);

  private buildQueryParams(params: BalanceHistoryQueryParameters): HttpParams {
    let httpParams = new HttpParams();
    if (params.balanceId !== undefined && params.balanceId !== null) {
      httpParams = httpParams.set('BalanceId', params.balanceId);
    }
    if (params.fromDate) httpParams = httpParams.set('FromDate', params.fromDate);
    if (params.toDate) httpParams = httpParams.set('ToDate', params.toDate);
    if (params.search) httpParams = httpParams.set('Search', params.search);
    if (params.pageNumber) httpParams = httpParams.set('PageNumber', params.pageNumber);
    if (params.pageSize) httpParams = httpParams.set('PageSize', params.pageSize);
    return httpParams;
  }

  getBalanceHistories(params: BalanceHistoryQueryParameters): Observable<PagedResult<BalanceHistoryDto>> {
    return this.http.get<PagedResult<BalanceHistoryDto>>(this.config.getEndpoint('/api/BalanceHistories'), {
      params: this.buildQueryParams(params)
    });
  }

  getById(id: number): Observable<BalanceHistoryDto> {
    return this.http.get<BalanceHistoryDto>(this.config.getEndpoint(`/api/BalanceHistories/${id}`));
  }

  getSummary(balanceId?: number, fromDate?: string, toDate?: string): Observable<import('../models/report.model').ReportSummaryData> {
    let httpParams = new HttpParams();
    if (balanceId !== undefined && balanceId !== null) {
      httpParams = httpParams.set('balanceId', balanceId);
    }
    if (fromDate) httpParams = httpParams.set('fromDate', fromDate);
    if (toDate) httpParams = httpParams.set('toDate', toDate);

    return this.http.get<import('../models/report.model').ReportSummaryData>(
      this.config.getEndpoint('/api/BalanceHistories/summary'),
      { params: httpParams }
    );
  }

  exportDirectPdf(params: BalanceHistoryQueryParameters): Observable<Blob> {
    return this.http.get(this.config.getEndpoint('/api/BalanceHistories/export-direct/pdf'), {
      params: this.buildQueryParams(params),
      responseType: 'blob'
    });
  }

  exportDirectExcel(params: BalanceHistoryQueryParameters): Observable<Blob> {
    return this.http.get(this.config.getEndpoint('/api/BalanceHistories/export-direct/excel'), {
      params: this.buildQueryParams(params),
      responseType: 'blob'
    });
  }
}
