import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';

// Angular CDK & Material Modules
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

// Core Services & Models
import { BalanceService } from './core/services/balance.service';
import { BalanceHistoryService } from './core/services/balance-history.service';
import { ReportService } from './core/services/report.service';
import { FileService } from './core/services/file.service';
import { SignalRService } from './core/services/signalr.service';
import { LoadingService } from './core/services/loading.service';
import { NotificationService } from './core/services/notification.service';
import { BalanceDto } from './core/models/balance.model';
import {
  BalanceHistoryDto,
  BalanceHistoryQueryParameters,
  ProjectedBalanceHistoryRow,
  projectBalanceHistoryRow
} from './core/models/balance-history.model';
import { ToastContainerComponent } from './shared/components/toast/toast.component';
import { TransactionDetailsDialogComponent } from './features/transaction-details-dialog/transaction-details-dialog.component';
import { GeneratedFilesDialogComponent } from './features/generated-files-dialog/generated-files-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ScrollingModule,
    MatToolbarModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatTableModule,
    MatPaginatorModule,
    MatDialogModule,
    ToastContainerComponent
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private balanceService = inject(BalanceService);
  private balanceHistoryService = inject(BalanceHistoryService);
  private reportService = inject(ReportService);
  private fileService = inject(FileService);
  private signalRService = inject(SignalRService);
  protected loadingService = inject(LoadingService);
  private dialog = inject(MatDialog);
  protected notificationService = inject(NotificationService);

  private pendingProcesses = new Set<string>();

  // Tab View Mode: 'grid' (Standard Paginated Grid) | 'infinite' (Infinite Scrolling with Paged Chunks)
  activeViewMode = signal<'grid' | 'infinite'>('grid');

  // Infinite Scrolling State (Paged Chunks + Low-Memory Data Projection)
  readonly INFINITE_CHUNK_SIZE = 50;
  infiniteProjectedRows = signal<ProjectedBalanceHistoryRow[]>([]);
  infinitePage = signal<number>(1);
  totalInfiniteCount = signal<number>(0);
  isLoadingChunk = signal<boolean>(false);
  hasMoreChunks = signal<boolean>(true);

  // Form Controls
  accountControl = new FormControl<string | BalanceDto | null>('');
  fromDateControl = new FormControl<Date | null>(null);
  toDateControl = new FormControl<Date | null>(null);
  gridSearchControl = new FormControl<string>('');

  // Table Direct Export State
  isDirectExportingPdf = signal<boolean>(false);
  isDirectExportingExcel = signal<boolean>(false);

  // Autocomplete state
  filteredAccounts = signal<BalanceDto[]>([]);
  isSearchingAccounts = signal<boolean>(false);
  isLoadingMoreAccounts = signal<boolean>(false);
  hasMoreAccounts = signal<boolean>(true);
  selectedAccount = signal<BalanceDto | null>(null);
  private currentSearchQuery = '';
  private accountPage = 1;

  // Task 3: Summary Totals State (retrieved from backend Stored Procedure)
  firstPreviousBalance = signal<number>(0);
  totalDebits = signal<number>(0);
  totalCredits = signal<number>(0);
  finalBalance = signal<number>(0);
  summaryAccountType = signal<string>('D');
  isLoadingSummary = signal<boolean>(false);

  // Task 4: Statement Table State & Columns
  readonly displayedColumns: string[] = [
    'balanceId',
    'accountName',
    'prevBalnce',
    'debtor',
    'creditor',
    'finalBalance',
    'actions'
  ];

  statementItems = signal<BalanceHistoryDto[]>([]);
  totalStatementCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = signal<number>(25);
  isLoadingStatement = signal<boolean>(false);
  hasSearched = signal<boolean>(false);

  // Computed Current Page Totals for Table Footer
  pageSumPrev = computed(() => {
    return this.statementItems().reduce((sum, item) => sum + (Number(item.prevBalnce) || 0), 0);
  });

  pageSumDebtor = computed(() => {
    return this.statementItems().reduce((sum, item) => sum + (Number(item.debtor) || 0), 0);
  });

  pageSumCreditor = computed(() => {
    return this.statementItems().reduce((sum, item) => sum + (Number(item.creditor) || 0), 0);
  });

  pageSumFinal = computed(() => {
    return this.statementItems().reduce((sum, item) => sum + (Number(item.calculatedFinalBalance) || 0), 0);
  });

  // Task 6: Background File Generation Signals
  isGeneratingPdf = signal<boolean>(false);
  isGeneratingExcel = signal<boolean>(false);
  generatedFilesCount = signal<number>(0);

  // Quick Test Preset Account
  readonly TEST_ACCOUNT: BalanceDto = {
    balanceId: 11038,
    parentCode: 11038,
    balanceName: 'Clients Class A',
    balanceType: 'D'
  };

  ngOnInit(): void {
    this.setupAccountAutocomplete();
    this.setupGridSearch();
    this.loadGeneratedFilesCount();
    this.setupSignalR();
  }

  private setupSignalR(): void {
    this.signalRService.startConnection();

    // Listen for file ready notifications from SignalR ReportHub
    this.signalRService.fileReady$.subscribe((notification) => {
      this.pendingProcesses.delete(notification.processId);
      this.isGeneratingPdf.set(false);
      this.isGeneratingExcel.set(false);

      this.notificationService.success(
        `Report "${notification.fileName}" is ready! Download started.`,
        'Report Ready'
      );
      this.loadGeneratedFilesCount();

      // Trigger file download directly
      const downloadUrl = notification.downloadUrl?.startsWith('http')
        ? notification.downloadUrl
        : this.fileService.getDownloadUrl(notification.processId);

      window.open(downloadUrl, '_blank');
    });

    // Listen for file failed notifications from SignalR ReportHub
    this.signalRService.fileFailed$.subscribe((notification) => {
      this.pendingProcesses.delete(notification.processId);
      this.isGeneratingPdf.set(false);
      this.isGeneratingExcel.set(false);

      this.notificationService.error(
        'Report generation failed. Please try again.',
        'Generation Failed'
      );
      this.loadGeneratedFilesCount();
    });
  }

  loadGeneratedFilesCount(): void {
    this.fileService.getFiles(1, 1).subscribe({
      next: (res) => this.generatedFilesCount.set(res.totalCount || 0),
      error: () => { }
    });
  }

  // --- Autocomplete Setup & Infinite Scroll ---
  private setupAccountAutocomplete(): void {
    this.fetchAccounts('', 1, false);

    this.accountControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe((value) => {
      if (typeof value === 'string') {
        this.currentSearchQuery = value.trim();
        this.accountPage = 1;
        this.fetchAccounts(this.currentSearchQuery, 1, false);
      }
    });
  }

  private fetchAccounts(search: string, page: number, append: boolean): void {
    if (append) {
      this.isLoadingMoreAccounts.set(true);
    } else {
      this.isSearchingAccounts.set(true);
    }

    this.balanceService.getBalances({ search, pageNumber: page, pageSize: 30 }).subscribe({
      next: (res) => {
        const items = res.items || [];
        if (append) {
          this.filteredAccounts.update(existing => [...existing, ...items]);
        } else {
          this.filteredAccounts.set(items);
        }

        const total = res.totalCount || 0;
        this.hasMoreAccounts.set((page * 30) < total);
        this.isSearchingAccounts.set(false);
        this.isLoadingMoreAccounts.set(false);
      },
      error: () => {
        this.isSearchingAccounts.set(false);
        this.isLoadingMoreAccounts.set(false);
      }
    });
  }

  // Attach infinite scroll listener when autocomplete panel opens
  onAutocompleteOpened(): void {
    const attachListener = () => {
      const panel = document.querySelector('.mat-mdc-autocomplete-panel') as HTMLElement;
      if (panel) {
        panel.onscroll = () => {
          const threshold = 40;
          const isAtBottom = panel.scrollTop + panel.clientHeight >= panel.scrollHeight - threshold;
          if (isAtBottom && this.hasMoreAccounts() && !this.isLoadingMoreAccounts() && !this.isSearchingAccounts()) {
            this.loadMoreAccounts();
          }
        };
      }
    };
    setTimeout(attachListener, 60);
    setTimeout(attachListener, 200);
  }

  loadMoreAccounts(): void {
    if (!this.hasMoreAccounts() || this.isLoadingMoreAccounts() || this.isSearchingAccounts()) return;
    this.accountPage++;
    this.fetchAccounts(this.currentSearchQuery, this.accountPage, true);
  }

  displayAccountFn(account: BalanceDto | string | null): string {
    if (!account) return '';
    if (typeof account === 'string') return account;
    const code = account.parentCode || account.balanceId;
    return `(${code} - ${account.balanceName || 'Unnamed'})`;
  }

  onAccountSelected(event: any): void {
    const account: BalanceDto = event.option.value;
    this.selectedAccount.set(account);
  }

  clearAccount(): void {
    this.accountControl.setValue('');
    this.selectedAccount.set(null);
    this.resetSummaries();
    this.statementItems.set([]);
    this.infiniteProjectedRows.set([]);
    this.totalStatementCount.set(0);
    this.totalInfiniteCount.set(0);
    this.infinitePage.set(1);
    this.hasMoreChunks.set(true);
    this.hasSearched.set(false);
    this.gridSearchControl.setValue('', { emitEvent: false });
  }

  // Quick Range Signal for UI pill active states
  selectedQuickRange = signal<string>('');

  // --- Quick Preset ---
  loadTestAccount(): void {
    this.accountControl.setValue(this.TEST_ACCOUNT);
    this.selectedAccount.set(this.TEST_ACCOUNT);
    this.onSearch();
  }

  // --- Quick Date Range Presets ---
  setLastMonth(): void {
    const now = new Date();
    const fromDate = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    const toDate = new Date(now.getFullYear(), now.getMonth(), 0);
    this.fromDateControl.setValue(fromDate);
    this.toDateControl.setValue(toDate);
    this.selectedQuickRange.set('last-month');
    this.onSearch();
  }

  setLastThreeMonths(): void {
    const now = new Date();
    const fromDate = new Date(now.getFullYear(), now.getMonth() - 3, now.getDate());
    const toDate = new Date();
    this.fromDateControl.setValue(fromDate);
    this.toDateControl.setValue(toDate);
    this.selectedQuickRange.set('last-3-months');
    this.onSearch();
  }

  setCurrentYear(): void {
    const currentYear = new Date().getFullYear();
    const fromDate = new Date(currentYear, 0, 1);
    const toDate = new Date();
    this.fromDateControl.setValue(fromDate);
    this.toDateControl.setValue(toDate);
    this.selectedQuickRange.set('current-year');
    this.onSearch();
  }

  setSince2000(): void {
    const fromDate = new Date(2000, 0, 1);
    const toDate = new Date();
    this.fromDateControl.setValue(fromDate);
    this.toDateControl.setValue(toDate);
    this.selectedQuickRange.set('since-2000');
    this.onSearch();
  }

  // --- Validation ---
  validateFilter(): boolean {
    const acc = this.selectedAccount();
    if (!acc) {
      this.notificationService.warning('Please select an account first from the list.', 'Account Required');
      return false;
    }

    const from = this.fromDateControl.value;
    const to = this.toDateControl.value;

    if (from && to && from > to) {
      this.notificationService.warning('"From Date" cannot be after "To Date".', 'Invalid Date Range');
      return false;
    }

    return true;
  }

  // --- Search Execution: Fetches Summary (Stored Proc) & Statement Table ---
  onSearch(): void {
    if (!this.validateFilter()) return;

    this.hasSearched.set(true);

    // 1. Fetch Summary Calculations from Backend Stored Procedure (dbo.sp_GetBalanceHistorySummary)
    this.fetchSummaryFromStoredProcedure();

    // 2. Search criteria changed: Reset both datasets
    this.statementItems.set([]);
    this.infiniteProjectedRows.set([]);
    this.infinitePage.set(1);
    this.hasMoreChunks.set(true);

    // 3. Fetch Active View Data
    if (this.activeViewMode() === 'infinite') {
      this.resetAndLoadInfinite();
    } else {
      this.currentPage.set(1);
      this.loadStatementPage(1);
    }
  }

  private fetchSummaryFromStoredProcedure(): void {
    const acc = this.selectedAccount();
    if (!acc) return;

    this.isLoadingSummary.set(true);
    const fromDateStr = this.formatDateParam(this.fromDateControl.value);
    const toDateStr = this.formatDateParam(this.toDateControl.value);

    this.balanceHistoryService.getSummary(acc.balanceId, fromDateStr, toDateStr).subscribe({
      next: (summary) => {
        this.isLoadingSummary.set(false);
        this.firstPreviousBalance.set(Number(summary.firstPreviousBalance) || 0);
        this.totalDebits.set(Number(summary.totalDebits) || 0);
        this.totalCredits.set(Number(summary.totalCredits) || 0);
        this.finalBalance.set(Number(summary.finalBalance) || 0);
        this.summaryAccountType.set(summary.accountType || acc.balanceType || 'D');
      },
      error: (err) => {
        this.isLoadingSummary.set(false);
        // console.error('Failed to load summary from stored procedure', err);
        this.resetSummaries();
      }
    });
  }

  // --- Task 4: Load Statement Data & Calculate Row Final Balance ---
  loadStatementPage(pageNumber: number): void {
    const acc = this.selectedAccount();
    if (!acc) return;

    this.isLoadingStatement.set(true);
    const fromDateStr = this.formatDateParam(this.fromDateControl.value);
    const toDateStr = this.formatDateParam(this.toDateControl.value);
    const searchTerm = this.gridSearchControl.value?.trim() || undefined;

    this.balanceHistoryService.getBalanceHistories({
      balanceId: acc.balanceId,
      fromDate: fromDateStr,
      toDate: toDateStr,
      search: searchTerm,
      pageNumber: pageNumber,
      pageSize: this.pageSize()
    }).subscribe({
      next: (res) => {
        this.isLoadingStatement.set(false);
        this.currentPage.set(pageNumber);
        this.totalStatementCount.set(res.totalCount || 0);

        const accountType = acc.balanceType || 'D';
        const isCreditAccount = (accountType === 'C' || accountType === 'Credit');

        // Apply row-level Final Balance calculation
        const processedItems = (res.items || []).map(item => {
          const prev = Number(item.prevBalnce) || 0;
          const debit = Number(item.debtor) || 0;
          const credit = Number(item.creditor) || 0;

          // Technical Requirement:
          // If account type = Debit -> ((Previous Balance + Debit) - Credit)
          // If account type = Credit -> ((Previous Balance + Credit) - Debit)
          const calculatedFinal = isCreditAccount
            ? (prev + credit) - debit
            : (prev + debit) - credit;

          return {
            ...item,
            calculatedFinalBalance: calculatedFinal
          };
        });

        this.statementItems.set(processedItems);

        if (pageNumber === 1 && res.totalCount === 0) {
          this.notificationService.info('No transactions found for the selected date range.', 'Statement Search');
        }
      },
      error: (err) => {
        this.isLoadingStatement.set(false);
        // console.error('Statement fetch failed', err);
        this.notificationService.error('Failed to load statement data. Please try again.', 'Search Error');
        this.statementItems.set([]);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.loadStatementPage(event.pageIndex + 1);
  }

  viewTransactionDetails(item: BalanceHistoryDto): void {
    const account = this.selectedAccount();
    this.dialog.open(TransactionDetailsDialogComponent, {
      data: {
        transaction: item,
        accountName: account?.balanceName,
        accountType: account?.balanceType
      },
      width: '600px',
      maxWidth: '95vw',
      panelClass: 'custom-financial-dialog',
      backdropClass: 'cdk-overlay-dark-backdrop'
    });
  }

  private resetSummaries(): void {
    this.firstPreviousBalance.set(0);
    this.totalDebits.set(0);
    this.totalCredits.set(0);
    this.finalBalance.set(0);
  }

  // --- Background File Generation & Tracking ---
  openFilesDialog(): void {
    const dialogRef = this.dialog.open(GeneratedFilesDialogComponent, {
      width: '760px',
      maxWidth: '95vw',
      panelClass: 'custom-financial-dialog',
      backdropClass: 'cdk-overlay-dark-backdrop'
    });

    dialogRef.afterClosed().subscribe(() => {
      this.loadGeneratedFilesCount();
    });
  }

  onPrintPdf(): void {
    if (this.isGeneratingPdf() || this.isGeneratingExcel()) {
      this.notificationService.warning('A report is currently being generated. Please wait until it completes.', 'Export in Progress');
      return;
    }
    if (!this.validateFilter()) return;
    const acc = this.selectedAccount()!;

    this.isGeneratingPdf.set(true);
    this.notificationService.info('Preparing your PDF report...', 'PDF Export');

    const fromDateStr = this.formatDateParam(this.fromDateControl.value);
    const toDateStr = this.formatDateParam(this.toDateControl.value);

    this.reportService.requestPdf({
      balanceId: acc.balanceId,
      fromDate: fromDateStr,
      toDate: toDateStr,
      connectionId: this.signalRService.connectionId || undefined
    }).subscribe({
      next: (res) => {
        this.pendingProcesses.add(res.processId);
        this.signalRService.subscribeToProcess(res.processId);
        this.loadGeneratedFilesCount();

        // Safety fallback timer: in case of unhandled server timeout
        setTimeout(() => {
          if (this.pendingProcesses.has(res.processId)) {
            this.pendingProcesses.delete(res.processId);
            this.isGeneratingPdf.set(false);
          }
        }, 60000);
      },
      error: (err) => {
        this.isGeneratingPdf.set(false);
        // console.error('PDF request failed', err);
        this.notificationService.error('Failed to generate PDF report. Please try again.', 'Export Error');
      }
    });
  }

  onExportExcel(): void {
    if (this.isGeneratingPdf() || this.isGeneratingExcel()) {
      this.notificationService.warning('A report is currently being generated. Please wait until it completes.', 'Export in Progress');
      return;
    }
    if (!this.validateFilter()) return;
    const acc = this.selectedAccount()!;

    this.isGeneratingExcel.set(true);
    this.notificationService.info('Preparing your Excel report...', 'Excel Export');

    const fromDateStr = this.formatDateParam(this.fromDateControl.value);
    const toDateStr = this.formatDateParam(this.toDateControl.value);

    this.reportService.requestExcel({
      balanceId: acc.balanceId,
      fromDate: fromDateStr,
      toDate: toDateStr,
      connectionId: this.signalRService.connectionId || undefined
    }).subscribe({
      next: (res) => {
        this.pendingProcesses.add(res.processId);
        this.signalRService.subscribeToProcess(res.processId);
        this.loadGeneratedFilesCount();

        // Safety fallback timer: in case of unhandled server timeout
        setTimeout(() => {
          if (this.pendingProcesses.has(res.processId)) {
            this.pendingProcesses.delete(res.processId);
            this.isGeneratingExcel.set(false);
          }
        }, 60000);
      },
      error: (err) => {
        this.isGeneratingExcel.set(false);
        // console.error('Excel request failed', err);
        this.notificationService.error('Failed to generate Excel report. Please try again.', 'Export Error');
      }
    });
  }

  formatDateParam(date: Date | null): string | undefined {
    if (!date) return undefined;
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // --- Grid Server-Side Search Setup ---
  private setupGridSearch(): void {
    this.gridSearchControl.valueChanges
      .pipe(
        debounceTime(350),
        distinctUntilChanged()
      )
      .subscribe(() => {
        if (this.selectedAccount()) {
          // Grid search changed -> Reset datasets
          this.statementItems.set([]);
          this.infiniteProjectedRows.set([]);
          this.infinitePage.set(1);
          this.hasMoreChunks.set(true);

          if (this.activeViewMode() === 'infinite') {
            this.resetAndLoadInfinite();
          } else {
            this.loadStatementPage(1);
          }
        }
      });
  }

  clearGridSearch(): void {
    this.gridSearchControl.setValue('');
  }

  // --- Tab Mode Switching: Preserves loaded data between tabs without deleting unless search changes ---
  switchViewMode(mode: 'grid' | 'infinite'): void {
    if (this.activeViewMode() === mode) return;
    this.activeViewMode.set(mode);

    // If switching to infinite and no chunks loaded yet, fetch initial chunk
    if (mode === 'infinite') {
      if (this.infiniteProjectedRows().length === 0 && this.selectedAccount() && this.hasSearched()) {
        this.resetAndLoadInfinite();
      }
    } else {
      // If switching to grid and no items loaded yet, fetch current page
      if (this.statementItems().length === 0 && this.selectedAccount() && this.hasSearched()) {
        this.loadStatementPage(this.currentPage());
      }
    }
  }

  // --- Reset and Load Infinite Scroll Chunks ---
  resetAndLoadInfinite(): void {
    const acc = this.selectedAccount();
    if (!acc) return;

    this.infiniteProjectedRows.set([]);
    this.infinitePage.set(1);
    this.hasMoreChunks.set(true);
    this.totalInfiniteCount.set(0);

    // Fetch summary calculations directly from stored procedure summary endpoint
    this.fetchSummaryFromStoredProcedure();

    // Fetch first chunk
    this.loadNextInfiniteChunk();
  }

  // --- Load Next Infinite Chunk with Data Projection (Stores ONLY required fields) ---
  loadNextInfiniteChunk(): void {
    const acc = this.selectedAccount();
    if (!acc || this.isLoadingChunk() || !this.hasMoreChunks()) return;

    this.isLoadingChunk.set(true);
    const pageToFetch = this.infinitePage();
    const fromDateStr = this.formatDateParam(this.fromDateControl.value);
    const toDateStr = this.formatDateParam(this.toDateControl.value);
    const searchTerm = this.gridSearchControl.value?.trim() || undefined;

    this.balanceHistoryService.getBalanceHistories({
      balanceId: acc.balanceId,
      fromDate: fromDateStr,
      toDate: toDateStr,
      search: searchTerm,
      pageNumber: pageToFetch,
      pageSize: this.INFINITE_CHUNK_SIZE
    }).subscribe({
      next: (res) => {
        this.isLoadingChunk.set(false);
        this.totalInfiniteCount.set(res.totalCount || 0);

        const accountType = acc.balanceType || 'D';
        const isCreditAccount = (accountType === 'C' || accountType === 'Credit');

        // DATA PROJECTION: Map to ProjectedBalanceHistoryRow (discards heavy unused fields)
        const projectedChunk = (res.items || []).map(dto => projectBalanceHistoryRow(dto, isCreditAccount));

        this.infiniteProjectedRows.update(existing => [...existing, ...projectedChunk]);
        this.infinitePage.set(pageToFetch + 1);

        const totalLoaded = this.infiniteProjectedRows().length;
        if (totalLoaded >= (res.totalCount || 0) || (res.items?.length || 0) < this.INFINITE_CHUNK_SIZE) {
          this.hasMoreChunks.set(false);
        }
      },
      error: () => {
        this.isLoadingChunk.set(false);
        this.notificationService.error('Failed to load transaction chunk.', 'Infinite Scroll Error');
      }
    });
  }

  // --- CDK Virtual Scroll Index Change Listener ---
  onInfiniteScrollIndexChange(firstVisibleIndex: number): void {
    const totalLoaded = this.infiniteProjectedRows().length;
    // When the user scrolls within 15 items of the end, automatically fetch next chunk
    if (totalLoaded > 0 && firstVisibleIndex + 15 >= totalLoaded && !this.isLoadingChunk() && this.hasMoreChunks()) {
      this.loadNextInfiniteChunk();
    }
  }

  trackByRowId(_index: number, row: ProjectedBalanceHistoryRow): number {
    return row.id;
  }

  viewProjectedTransactionDetails(row: ProjectedBalanceHistoryRow): void {
    const account = this.selectedAccount();
    this.balanceHistoryService.getById(row.id).subscribe({
      next: (dto) => {
        this.dialog.open(TransactionDetailsDialogComponent, {
          data: {
            transaction: {
              ...dto,
              calculatedFinalBalance: row.final
            },
            accountName: account?.balanceName,
            accountType: account?.balanceType
          },
          width: '680px',
          panelClass: 'custom-dialog-panel'
        });
      },
      error: () => {
        this.dialog.open(TransactionDetailsDialogComponent, {
          data: {
            transaction: {
              balanceHisId: row.id,
              date: row.date,
              orderBill: row.orderBill,
              kind: row.kind,
              prevBalnce: row.prev,
              debtor: row.debit,
              creditor: row.credit,
              calculatedFinalBalance: row.final
            } as BalanceHistoryDto,
            accountName: account?.balanceName,
            accountType: account?.balanceType
          },
          width: '680px',
          panelClass: 'custom-dialog-panel'
        });
      }
    });
  }

  // --- Direct In-Memory Export Handlers (Single Page, Immediate Download / Open) ---
  onDirectExportPdf(): void {
    if (this.isDirectExportingPdf() || this.isDirectExportingExcel()) {
      return;
    }
    const acc = this.selectedAccount();
    if (!acc || this.statementItems().length === 0) {
      this.notificationService.warning('No statement transactions available to export.', 'Export PDF');
      return;
    }

    this.isDirectExportingPdf.set(true);
    const fromDateStr = this.formatDateParam(this.fromDateControl.value);
    const toDateStr = this.formatDateParam(this.toDateControl.value);
    const searchTerm = this.gridSearchControl.value?.trim() || undefined;

    const params: BalanceHistoryQueryParameters = {
      balanceId: acc.balanceId,
      fromDate: fromDateStr,
      toDate: toDateStr,
      search: searchTerm,
      pageNumber: this.currentPage(),
      pageSize: this.pageSize()
    };

    this.balanceHistoryService.exportDirectPdf(params).subscribe({
      next: (blob) => {
        this.isDirectExportingPdf.set(false);
        const blobUrl = window.URL.createObjectURL(blob);
        window.open(blobUrl, '_blank');
        setTimeout(() => window.URL.revokeObjectURL(blobUrl), 10000);
        this.notificationService.success('PDF opened in a new tab.', 'Direct Export');
      },
      error: (err) => {
        this.isDirectExportingPdf.set(false);
        // console.error('Direct PDF export error', err);
        this.notificationService.error('Failed to export PDF.', 'Export Error');
      }
    });
  }

  onDirectExportExcel(): void {
    if (this.isDirectExportingPdf() || this.isDirectExportingExcel()) {
      return;
    }
    const acc = this.selectedAccount();
    if (!acc || this.statementItems().length === 0) {
      this.notificationService.warning('No statement transactions available to export.', 'Export Excel');
      return;
    }

    this.isDirectExportingExcel.set(true);
    const fromDateStr = this.formatDateParam(this.fromDateControl.value);
    const toDateStr = this.formatDateParam(this.toDateControl.value);
    const searchTerm = this.gridSearchControl.value?.trim() || undefined;

    const params: BalanceHistoryQueryParameters = {
      balanceId: acc.balanceId,
      fromDate: fromDateStr,
      toDate: toDateStr,
      search: searchTerm,
      pageNumber: this.currentPage(),
      pageSize: this.pageSize()
    };

    this.balanceHistoryService.exportDirectExcel(params).subscribe({
      next: (blob) => {
        this.isDirectExportingExcel.set(false);
        const blobUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = blobUrl;
        link.download = `Statement_Page_${this.currentPage()}_${new Date().getTime()}.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(blobUrl);
        this.notificationService.success('Excel spreadsheet downloaded directly.', 'Direct Export');
      },
      error: (err) => {
        this.isDirectExportingExcel.set(false);
        // console.error('Direct Excel export error', err);
        this.notificationService.error('Failed to export Excel.', 'Export Error');
      }
    });
  }
}
