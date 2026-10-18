import { Injectable, signal, computed } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private activeRequests = signal<number>(0);

  /**
   * Computed boolean signal indicating whether any HTTP requests or operations are currently active.
   */
  readonly isLoading = computed(() => this.activeRequests() > 0);

  /**
   * Increments the in-flight request counter.
   */
  show(): void {
    this.activeRequests.update(count => count + 1);
  }

  /**
   * Decrements the in-flight request counter safely without dropping below zero.
   */
  hide(): void {
    this.activeRequests.update(count => Math.max(0, count - 1));
  }

  /**
   * Resets active requests to 0.
   */
  reset(): void {
    this.activeRequests.set(0);
  }
}
