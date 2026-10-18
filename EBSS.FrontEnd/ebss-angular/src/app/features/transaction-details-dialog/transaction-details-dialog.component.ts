import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BalanceHistoryDto } from '../../core/models/balance-history.model';

export interface TransactionDialogData {
  transaction: BalanceHistoryDto;
  accountName?: string;
  accountType?: string;
}

@Component({
  selector: 'app-transaction-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatTooltipModule
  ],
  template: `
    <div class="tx-modal-wrapper">
      <!-- Fixed Header -->
      <div class="modal-header">
        <div class="modal-header-left">
          <div class="modal-icon-tile">
            <mat-icon>receipt</mat-icon>
          </div>
          <div class="modal-title-col">
            <h2 class="modal-title">Transaction #{{ data.transaction.balanceHisId }}</h2>
            <span class="modal-subtitle">
              {{ data.accountName || 'Clients Class A' }}
              @if (data.transaction.date) {
                · {{ data.transaction.date | date:'mediumDate' }}
              }
            </span>
          </div>
        </div>
        <button type="button" class="btn-modal-close" (click)="dialogRef.close()" matTooltip="Close">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="modal-divider"></div>

      <!-- Scrollable Body -->
      <div class="modal-body-scroll">
        <!-- 4 Mini Metric Cards -->
        <div class="metrics-cards-container">
          <!-- Top 3 Cards -->
          <div class="metrics-row-top">
            <div class="mini-card card-debit">
              <span class="mini-label">Debit Amount</span>
              <span class="mini-value font-mono color-debit">
                {{ (data.transaction.debtor || 0) | number:'1.2-2' }}
              </span>
            </div>

            <div class="mini-card card-credit">
              <span class="mini-label">Credit Amount</span>
              <span class="mini-value font-mono color-credit">
                {{ (data.transaction.creditor || 0) | number:'1.2-2' }}
              </span>
            </div>

            <div class="mini-card card-prev">
              <span class="mini-label">Previous Balance</span>
              <span class="mini-value font-mono color-prev">
                {{ (data.transaction.prevBalnce || 0) | number:'1.2-2' }}
              </span>
            </div>
          </div>

          <!-- Bottom Full Width Card (New Balance - Dark Navy) -->
          <div class="mini-card card-new-balance">
            <span class="mini-label label-new-dark">New Balance</span>
            <span class="mini-value font-mono value-new-dark">
              {{ (data.transaction.newBalnce || 0) | number:'1.2-2' }}
            </span>
          </div>
        </div>

        <!-- Section 1: Transaction Information -->
        <div class="details-section-box">
          <h3 class="section-box-title">TRANSACTION INFORMATION</h3>

          <div class="kv-rows-list">
            <div class="kv-row">
              <span class="kv-label">Transaction ID</span>
              <span class="kv-value font-mono">{{ data.transaction.balanceHisId }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Account ID</span>
              <span class="kv-value font-mono">{{ data.transaction.balanceId || '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Request No</span>
              <span class="kv-value font-mono">{{ data.transaction.requestNo ?? '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Type / Kind</span>
              <span class="kv-value">{{ data.transaction.kind || '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Order Bill</span>
              <span class="kv-value font-mono">{{ data.transaction.orderBill || '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Bill Arrange</span>
              <span class="kv-value font-mono">{{ data.transaction.billArrange ?? '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Reference No</span>
              <span class="kv-value">{{ data.transaction.referenceNo || '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Branch ID</span>
              <span class="kv-value font-mono">{{ data.transaction.branchId ?? '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Cost Center ID</span>
              <span class="kv-value font-mono">{{ data.transaction.costCenterId ?? '-' }}</span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Organization ID</span>
              <span class="kv-value font-mono">{{ data.transaction.orgId ?? '-' }}</span>
            </div>
          </div>
        </div>

        <!-- Section 2: Dates and Timestamps -->
        <div class="details-section-box mt-3">
          <h3 class="section-box-title">DATES AND TIMESTAMPS</h3>

          <div class="kv-rows-list">
            <div class="kv-row">
              <span class="kv-label">Transaction Date</span>
              <span class="kv-value">
                {{ data.transaction.date ? (data.transaction.date | date:'mediumDate') : '-' }}
              </span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Insert Date</span>
              <span class="kv-value">
                {{ data.transaction.insertDate ? (data.transaction.insertDate | date:'medium') : '-' }}
              </span>
            </div>

            <div class="kv-row">
              <span class="kv-label">Due Date (Este7kak)</span>
              <span class="kv-value">
                {{ data.transaction.este7kakDate ? (data.transaction.este7kakDate | date:'mediumDate') : '-' }}
              </span>
            </div>
          </div>
        </div>

        <!-- Section 3: Remarks (HTML) -->
        @if (data.transaction.remarks) {
          <div class="details-section-box mt-3">
            <h3 class="section-box-title">REMARKS / DESCRIPTION</h3>
            <div class="remarks-html-box" [innerHTML]="data.transaction.remarks"></div>
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
    .tx-modal-wrapper {
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
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    /* 4 Mini Cards */
    .metrics-cards-container {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .metrics-row-top {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 10px;
    }

    .mini-card {
      padding: 14px 16px;
      border-radius: 12px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .card-debit {
      background-color: #EBF3FF;
    }

    .card-credit {
      background-color: #FFF3E8;
    }

    .card-prev {
      background-color: #F1F4F8;
    }

    .card-new-balance {
      background-color: #0F2A43;
      padding: 16px 20px;
    }

    .mini-label {
      font-size: 0.78rem;
      font-weight: 500;
      color: #4F5D6B;
    }

    .mini-value {
      font-size: 1.35rem;
      font-weight: 600;
      line-height: 1.2;
    }

    .color-debit {
      color: #1A4FAE;
    }

    .color-credit {
      color: #A84A00;
    }

    .color-prev {
      color: #33414E;
    }

    .label-new-dark {
      color: rgba(255, 255, 255, 0.75);
    }

    .value-new-dark {
      color: #ffffff;
      font-size: 1.45rem;
    }

    /* Grouped Information Boxes */
    .details-section-box {
      border: 1px solid #E3E7EC;
      border-radius: 12px;
      padding: 16px 20px;
      background-color: #ffffff;
    }

    .section-box-title {
      margin: 0 0 12px 0;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #4F5D6B;
    }

    .kv-rows-list {
      display: flex;
      flex-direction: column;
    }

    .kv-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 10px 0;
      border-bottom: 1px solid #F1F4F8;
      font-size: 0.88rem;
    }

    .kv-row:last-child {
      border-bottom: none;
      padding-bottom: 0;
    }

    .kv-row:first-child {
      padding-top: 0;
    }

    .kv-label {
      color: #4F5D6B;
      font-weight: 400;
    }

    .kv-value {
      color: #1E293B;
      font-weight: 500;
    }

    .remarks-html-box {
      font-size: 0.88rem;
      color: #33414E;
      line-height: 1.5;
      padding-top: 4px;
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

    .mt-3 {
      margin-top: 12px;
    }
  `]
})
export class TransactionDetailsDialogComponent {
  dialogRef = inject(MatDialogRef<TransactionDetailsDialogComponent>);
  data: TransactionDialogData = inject(MAT_DIALOG_DATA);
}
