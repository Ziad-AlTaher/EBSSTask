import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

/**
 * Functional HTTP loading interceptor that tracks all outgoing HTTP requests
 * and informs LoadingService during their lifecycle.
 */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  // Notify LoadingService of new in-flight HTTP request
  loadingService.show();

  return next(req).pipe(
    finalize(() => {
      // Decrement when request concludes (success, error, or cancelled)
      loadingService.hide();
    })
  );
};
