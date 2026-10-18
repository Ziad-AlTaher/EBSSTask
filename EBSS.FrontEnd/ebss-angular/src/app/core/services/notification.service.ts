import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: string;
  title?: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  readonly toasts = signal<ToastMessage[]>([]);

  show(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info', title?: string, duration: number = 4000): void {
    const id = Math.random().toString(36).substring(2, 9);
    const toast: ToastMessage = { id, message, type, title, duration };

    this.toasts.update(current => [...current, toast]);

    if (duration > 0) {
      setTimeout(() => {
        this.dismiss(id);
      }, duration);
    }
  }

  success(message: string, title: string = 'Success'): void {
    this.show(message, 'success', title);
  }

  error(message: string, title: string = 'Error'): void {
    this.show(message, 'error', title, 5000);
  }

  warning(message: string, title: string = 'Warning'): void {
    this.show(message, 'warning', title, 4500);
  }

  info(message: string, title: string = 'Notice'): void {
    this.show(message, 'info', title);
  }

  dismiss(id: string): void {
    this.toasts.update(current => current.filter(t => t.id !== id));
  }
}
