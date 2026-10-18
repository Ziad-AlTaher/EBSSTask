import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  template: `
    <div class="toast-container">
      @for (toast of notificationService.toasts(); track toast.id) {
        <div class="toast-card" [ngClass]="'toast-' + toast.type">
          <div class="toast-icon">
            <mat-icon>{{ getIcon(toast.type) }}</mat-icon>
          </div>
          <div class="toast-body">
            @if (toast.title) {
              <div class="toast-title">{{ toast.title }}</div>
            }
            <div class="toast-message">{{ toast.message }}</div>
          </div>
          <button mat-icon-button class="toast-close" (click)="notificationService.dismiss(toast.id)">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 12px;
      max-width: 420px;
      width: calc(100% - 48px);
      pointer-events: none;
    }

    .toast-card {
      pointer-events: auto;
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 14px 16px;
      border-radius: 10px;
      background: white;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
      border-left: 5px solid #64748b;
      animation: slideIn 0.25s cubic-bezier(0.16, 1, 0.3, 1);
    }

    @keyframes slideIn {
      from {
        transform: translateY(20px);
        opacity: 0;
      }
      to {
        transform: translateY(0);
        opacity: 1;
      }
    }

    .toast-success {
      border-left-color: #10b981;
    }
    .toast-success .toast-icon {
      color: #10b981;
    }

    .toast-error {
      border-left-color: #ef4444;
    }
    .toast-error .toast-icon {
      color: #ef4444;
    }

    .toast-warning {
      border-left-color: #f59e0b;
    }
    .toast-warning .toast-icon {
      color: #f59e0b;
    }

    .toast-info {
      border-left-color: #3b82f6;
    }
    .toast-info .toast-icon {
      color: #3b82f6;
    }

    .toast-icon mat-icon {
      font-size: 24px;
      width: 24px;
      height: 24px;
    }

    .toast-body {
      flex: 1;
      min-width: 0;
    }

    .toast-title {
      font-weight: 600;
      font-size: 0.9rem;
      color: #1e293b;
      margin-bottom: 2px;
    }

    .toast-message {
      font-size: 0.85rem;
      color: #475569;
      line-height: 1.35;
      word-break: break-word;
    }

    .toast-close {
      width: 28px;
      height: 28px;
      line-height: 28px;
      opacity: 0.6;
    }
    .toast-close:hover {
      opacity: 1;
    }
    .toast-close mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }
  `]
})
export class ToastContainerComponent {
  notificationService = inject(NotificationService);

  getIcon(type: string): string {
    switch (type) {
      case 'success': return 'check_circle';
      case 'error': return 'error';
      case 'warning': return 'warning';
      case 'info':
      default: return 'info';
    }
  }
}
