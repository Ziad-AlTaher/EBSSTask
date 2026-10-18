import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiConfigService } from './api-config.service';
import { FileProcessStatusDto } from '../models/file-process.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private http = inject(HttpClient);
  private config = inject(ApiConfigService);

  getFiles(pageNumber: number = 1, pageSize: number = 20): Observable<PagedResult<FileProcessStatusDto>> {
    const params = new HttpParams()
      .set('PageNumber', pageNumber)
      .set('PageSize', pageSize);

    return this.http.get<PagedResult<FileProcessStatusDto>>(this.config.getEndpoint('/api/files'), { params });
  }

  getFileStatus(processId: string): Observable<FileProcessStatusDto> {
    return this.http.get<FileProcessStatusDto>(this.config.getEndpoint(`/api/files/${processId}`));
  }

  getDownloadUrl(processId: string): string {
    return this.config.getEndpoint(`/api/files/${processId}/download`);
  }
}
