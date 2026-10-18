import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FileService } from '../../core/services/file.service';
import { SignalRService } from '../../core/services/signalr.service';
import {
  FileProcessStatusDto,
  getFileProcessStatusText,
  getReportFormatText
} from '../../core/models/file-process.model';

@Component({
  selector: 'app-generated-files-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  template: `
    <div class="files-modal-wrapper">
      <!-- Fixed Header -->
      <div class="modal-header">
        <div class="modal-header-left">
          <div class="modal-icon-tile">
            <mat-icon>folder_open</mat-icon>
          </div>
          <div class="modal-title-col">
            <h2 class="modal-title">Generated Files and Reports</h2>
            <span class="modal-subtitle">Background file generation history and status</span>
          </div>
        </div>
        <button type="button" class="btn-modal-close" (click)="dialogRef.close()" matTooltip="Close">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="modal-divider"></div>

      <!-- Scrollable Body -->
      <div class="modal-body-scroll">
        @if (isLoading()) {
          <div class="modal-loading-box">
            <mat-progress-spinner diameter="36" mode="indeterminate"></mat-progress-spinner>
            <span>Loading files history...</span>
          </div>
        } @else if (files().length === 0) {
          <div class="modal-empty-box">
            <mat-icon class="empty-ico">folder_open</mat-icon>
            <span>No generated files found yet.</span>
          </div>
        } @else {
          <div class="files-card-list">
            @for (file of files(); track file.id) {
              <div class="file-card-row">
                <!-- Left: 44px Format Badge & File Info -->
                <div class="file-left-group">
                  <div
                    class="format-square-badge"
                    [class.badge-pdf]="!isExcel(file.reportFormat)"
                    [class.badge-xls]="isExcel(file.reportFormat)"
                  >
                    {{ isExcel(file.reportFormat) ? 'XLS' : 'PDF' }}
                  </div>

                  <div class="file-details-col">
                    <div class="file-title-line">
                      Balance History · {{ isExcel(file.reportFormat) ? 'Excel (.xlsx)' : 'PDF (QuestPDF)' }}
                    </div>
                    <div class="file-process-id font-mono">
                      {{ file.id.substring(0, 8) }}...
                    </div>
                    <div class="file-dates-line">
                      Created {{ file.createdAt | date:'short' }} · Completed {{ file.completedAt ? (file.completedAt | date:'short') : 'Pending' }}
                    </div>
                  </div>
                </div>

                <!-- Right: Status Pill & Download Button -->
                <div class="file-right-group">
                  @if (isCompleted(file.processStatus)) {
                    <span class="status-pill status-completed">
                      <mat-icon class="status-ico">check</mat-icon>
                      Completed
                    </span>
                    <a
                      [href]="fileService.getDownloadUrl(file.id)"
                      target="_blank"
                      download
                      class="btn-download-outline"
                    >
                      <mat-icon>download</mat-icon>
                      <span>Download</span>
                    </a>
                  } @else if (isFailed(file.processStatus)) {
                    <span class="status-pill status-failed" [matTooltip]="file.errorMessage || 'Generation failed'">
                      <mat-icon class="status-ico">error_outline</mat-icon>
                      Failed
                    </span>
                  } @else {
                    <span class="status-pill status-processing">
                      <mat-progress-spinner diameter="14" mode="indeterminate" class="status-spinner"></mat-progress-spinner>
                      Processing
                    </span>
                  }
                </div>
              </div>
            }
          </div>
        }
      </div>

      <div class="modal-divider"></div>

      <!-- Fixed Footer -->
      <div class="modal-footer">
        <button type="button" class="btn-modal-action-close" (click)="dialogRef.close()">
          Close
        </button>
      </div>
    </div>
  `,
  styles: [`
    .files-modal-wrapper {
      display: flex;
      flex-direction: column;
      max-height: 85vh;
      background: #ffffff;
      border-radius: 20px;
      overflow: hidden;
    }

    .modal-header {
      padding: 20px 24px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-shrink: 0;
    }

    .modal-header-left {
      display: flex;
      align-items: center;
      gap: 14px;
    }

    .modal-icon-tile {
      width: 44px;
      height: 44px;
      border-radius: 12px;
      background-color: #E6EFF8;
      color: #0F4C81;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .modal-icon-tile mat-icon {
      font-size: 24px;
      width: 24px;
      height: 24px;
    }

    .modal-title-col {
      display: flex;
      flex-direction: column;
    }

    .modal-title {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 700;
      color: #0F2A43;
      line-height: 1.2;
    }

    .modal-subtitle {
      font-size: 0.82rem;
      color: #64748B;
      line-height: 1.3;
    }

    .btn-modal-close {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background-color: #F1F4F8;
      border: none;
      color: #475569;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      transition: background-color 0.15s ease;
    }

    .btn-modal-close:hover {
      background-color: #E2E8F0;
      color: #1E293B;
    }

    .btn-modal-close mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .modal-divider {
      height: 1px;
      background-color: #E3E7EC;
      width: 100%;
      flex-shrink: 0;
    }

    .modal-body-scroll {
      padding: 20px 24px;
      overflow-y: auto;
      max-height: calc(85vh - 160px);
    }

    .modal-loading-box,
    .modal-empty-box {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      padding: 48px;
      color: #64748B;
    }

    .empty-ico {
      font-size: 40px;
      width: 40px;
      height: 40px;
      color: #94A3B8;
    }

    .files-card-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    /* Single File Row Card */
    .file-card-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 14px 18px;
      border: 1px solid #E3E7EC;
      border-radius: 12px;
      background-color: #ffffff;
      transition: border-color 0.15s ease, box-shadow 0.15s ease;
    }

    .file-card-row:hover {
      border-color: #C9D1DA;
      box-shadow: 0 2px 6px rgba(15, 35, 55, 0.04);
    }

    .file-left-group {
      display: flex;
      align-items: center;
      gap: 14px;
    }

    .format-square-badge {
      width: 44px;
      height: 44px;
      border-radius: 10px;
      font-size: 0.85rem;
      font-weight: 700;
      font-family: var(--font-mono);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .badge-pdf {
      background-color: #FEEAEA;
      color: #B3261E;
    }

    .badge-xls {
      background-color: #EAF6EE;
      color: #1B6B3A;
    }

    .file-details-col {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .file-title-line {
      font-size: 0.94rem;
      font-weight: 600;
      color: #0F2A43;
    }

    .file-process-id {
      font-size: 0.8rem;
      color: #64748B;
    }

    .file-dates-line {
      font-size: 0.78rem;
      color: #8795A3;
    }

    .file-right-group {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .status-pill {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 4px 12px;
      border-radius: 14px;
      font-size: 0.82rem;
      font-weight: 600;
    }

    .status-ico {
      font-size: 15px;
      width: 15px;
      height: 15px;
    }

    .status-completed {
      background-color: #EAF6EE;
      color: #1B6B3A;
    }

    .status-processing {
      background-color: #FFF8E1;
      color: #B78103;
    }

    .status-failed {
      background-color: #FEEAEA;
      color: #B3261E;
    }

    .status-spinner ::ng-deep circle {
      stroke: #B78103 !important;
    }

    .btn-download-outline {
      height: 38px;
      padding: 0 16px;
      border-radius: 10px;
      border: 1px solid #C9D1DA;
      background-color: #ffffff;
      color: #0F4C81;
      font-size: 0.86rem;
      font-weight: 600;
      display: inline-flex;
      align-items: center;
      gap: 6px;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .btn-download-outline:hover {
      background-color: #F8FAFC;
      border-color: #0F4C81;
    }

    .btn-download-outline mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }

    .modal-footer {
      padding: 16px 24px;
      display: flex;
      justify-content: flex-end;
      flex-shrink: 0;
    }

    .btn-modal-action-close {
      height: 42px;
      padding: 0 24px;
      border-radius: 12px;
      background-color: #0F4C81;
      color: #ffffff;
      font-size: 0.92rem;
      font-weight: 600;
      border: none;
      cursor: pointer;
      transition: background-color 0.15s ease;
    }

    .btn-modal-action-close:hover {
      background-color: #0a365c;
    }
  `]
})
export class GeneratedFilesDialogComponent implements OnInit, OnDestroy {
  dialogRef = inject(MatDialogRef<GeneratedFilesDialogComponent>);
  protected fileService = inject(FileService);
  private signalRService = inject(SignalRService);

  files = signal<FileProcessStatusDto[]>([]);
  isLoading = signal<boolean>(false);
  private sub = new Subscription();

  ngOnInit(): void {
    this.loadFiles();

    // Auto-refresh files list whenever a background job completes or fails via SignalR
    this.sub.add(
      this.signalRService.fileReady$.subscribe(() => {
        this.loadFiles();
      })
    );

    this.sub.add(
      this.signalRService.fileFailed$.subscribe(() => {
        this.loadFiles();
      })
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  loadFiles(): void {
    this.isLoading.set(true);
    this.fileService.getFiles(1, 50).subscribe({
      next: (res) => {
        this.files.set(res.items || []);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  getStatusText(status: any): string {
    return getFileProcessStatusText(status);
  }

  getReportFormat(format: any): string {
    return getReportFormatText(format);
  }

  isExcel(format: any): boolean {
    const text = getReportFormatText(format).toLowerCase();
    return text.includes('excel') || format === 1 || format === '1';
  }

  isCompleted(status: any): boolean {
    const text = getFileProcessStatusText(status);
    return text === 'Completed';
  }

  isFailed(status: any): boolean {
    const text = getFileProcessStatusText(status);
    return text === 'Failed';
  }
}
