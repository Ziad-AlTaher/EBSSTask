import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiConfigService } from './api-config.service';
import { BalanceDto, BalanceQueryParameters } from '../models/balance.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({
  providedIn: 'root'
})
export class BalanceService {
  private http = inject(HttpClient);
  private config = inject(ApiConfigService);

  getBalances(params?: BalanceQueryParameters): Observable<PagedResult<BalanceDto>> {
    let httpParams = new HttpParams();
    if (params?.pageNumber) httpParams = httpParams.set('PageNumber', params.pageNumber);
    if (params?.pageSize) httpParams = httpParams.set('PageSize', params.pageSize);
    if (params?.search) httpParams = httpParams.set('Search', params.search);

    return this.http.get<PagedResult<BalanceDto>>(this.config.getEndpoint('/api/Balances'), {
      params: httpParams
    });
  }

  getBalanceById(id: number): Observable<BalanceDto> {
    return this.http.get<BalanceDto>(this.config.getEndpoint(`/api/Balances/${id}`));
  }
}
