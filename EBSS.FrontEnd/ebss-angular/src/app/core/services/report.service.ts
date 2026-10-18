import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiConfigService } from './api-config.service';
import { ReportRequestParameters, ReportSubmissionResult } from '../models/report.model';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private http = inject(HttpClient);
  private config = inject(ApiConfigService);

  requestExcel(params: ReportRequestParameters): Observable<ReportSubmissionResult> {
    return this.http.post<ReportSubmissionResult>(
      this.config.getEndpoint('/api/reports/balance-history/excel'),
      params
    );
  }

  requestPdf(params: ReportRequestParameters): Observable<ReportSubmissionResult> {
    return this.http.post<ReportSubmissionResult>(
      this.config.getEndpoint('/api/reports/balance-history/pdf'),
      params
    );
  }
}
