# EBSS Account Statement System — Comprehensive Technical Documentation

---

## 1. Executive Summary

The **EBSS Account Statement System** is an enterprise-grade financial reporting and transaction analysis application. It provides accounting professionals with real-time balance inquiries, server-side paginated transaction audits, formula-driven financial summaries, and a dual-tier report generation pipeline (asynchronous full-dataset background processing via SignalR vs. instantaneous single-page direct exports).

Built on **.NET 10** following **Clean Architecture principles** and paired with a modern **Angular 19** frontend (plus an alternative lightweight client), this document details every technical module, algorithm, architectural pattern, and database interaction across the entire solution.

---

## 2. Architecture Overview & Tech Stack

### 2.1 Solution Layers & Separation of Concerns

```
                     ┌──────────────────────────────────────┐
                     │           Presentation Layer         │
                     │  EBSS.FrontEnd (Angular 19 / Simple) │
                     └───────────────────┬──────────────────┘
                                         │ HTTP REST / SignalR WebSockets
                                         ▼
                     ┌──────────────────────────────────────┐
                     │              API Layer               │
                     │             EBSSTask.API             │
                     │  Controllers, Hubs, Middlewares, DI  │
                     └───────────────────┬──────────────────┘
                                         │
                                         ▼
                     ┌──────────────────────────────────────┐
                     │          Application Layer           │
                     │         EBSSTask.Application        │
                     │  DTOs, Services, Interfaces, Queries │
                     └─────────┬──────────────────┬─────────┘
                               │                  │
                               ▼                  ▼
┌──────────────────────────────────────┐  ┌──────────────────────────────────────┐
│         Infrastructure Layer         │  │             Domain Layer             │
│        EBSSTask.Infrastructure       │  │           EBSSTask.Domain            │
│ EF Core, Channels, Writers, SignalR  │  │ Entities, Enums, Value Objects       │
└──────────────────────────────────────┘  └──────────────────────────────────────┘
```

| Layer | Responsibility | Key Libraries & Technologies |
|---|---|---|
| **EBSSTask.Domain** | Core enterprise entities, enums, database schema models. Zero external dependencies. | C# 13, .NET 10 |
| **EBSSTask.Application** | Business rules, calculations, query builders, service abstractions, DTO definitions, report contracts. | FluentValidation, ClosedXML, QuestPDF, System.Threading.Channels |
| **EBSSTask.Infrastructure**| EF Core DbContext, SQL Server migrations, Background Service workers, Channel Queue, ClosedXML/QuestPDF report implementations. | Microsoft.EntityFrameworkCore.SqlServer, QuestPDF, ClosedXML |
| **EBSSTask.API** | REST controllers, SignalR Hubs, Swagger/OpenAPI, CORS policies, Exception Handling middleware. | ASP.NET Core 10, SignalR, Swashbuckle |
| **EBSS.FrontEnd** | Angular 19 SPA (Signals, Standalone Components, Material UI) & Vanilla JS fallback client. | Angular 19, TypeScript, RxJS, Angular Material |

### 2.2 Complete Inventory of All Used Libraries & Dependencies

#### A. Backend NuGet Packages (.NET 10)

| Package Name | Version | Projects | Purpose & Technical Justification |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore` | `10.0.13` | Application, Infrastructure | Core Object-Relational Mapper (ORM) providing `DbSet`, LINQ queries, and tracking. |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.13` | Infrastructure | SQL Server database provider for EF Core; generates optimized T-SQL for queries and Stored Procedures. |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.13` | Infrastructure | PowerShell and Package Manager Console commands for EF Core migrations. |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.13` | API, Infrastructure | Design-time components for EF Core migration generation and database scaffolding. |
| `QuestPDF` | `2026.9.1` | Application | Code-first, fluent C# PDF generation library used for both asynchronous reports and instantaneous direct exports. |
| `ClosedXML` | `0.105.1` | Application | OpenXML-based Excel generation library used to build `.xlsx` spreadsheets with styles, formulas, and auto-sizing without Excel interop. |
| `FluentValidation.AspNetCore` | `11.3.1` | API | Automatic request validation integrated with ASP.NET Core MVC model binding. |
| `FluentValidation.DependencyInjectionExtensions` | `12.2.0` | Application | Dependency injection registration scanning for custom validators (e.g., date ranges, query parameters). |
| `Swashbuckle.AspNetCore` | `10.3.0` | API | Generates Swagger 2.0 / OpenAPI 3.0 interactive documentation and test UI for all endpoints. |
| `Microsoft.AspNetCore.OpenApi` | `10.0.12` | API | Native ASP.NET Core OpenAPI metadata and endpoint specification generator. |
| `Microsoft.Extensions.Hosting.Abstractions` | `10.0.13` | Infrastructure | Abstractions for background hosted services (`BackgroundService`, `IHostedService`). |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | `1.24.2-preview.1` | API | Visual Studio tools for Docker containerization and debugging. |
| `System.Threading.Channels` | *Runtime Built-in* | Infrastructure | High-performance, thread-safe Producer-Consumer queue used by `ChannelReportQueue`. |
| `Microsoft.AspNetCore.SignalR` | *SDK Built-in* | API, Infrastructure | WebSocket and real-time push notification engine for asynchronous file completion alerts. |

#### B. Frontend npm Packages (Angular 19 SPA)

| Package Name | Version | Category | Purpose & Technical Justification |
|---|---|---|---|
| `@microsoft/signalr` | `^10.0.11` | Real-time Communication | JavaScript/TypeScript client library for connecting to the backend `ReportHub` WebSockets. |
| `@angular/material` | `^21.2.14` | UI Component System | Material Design component library (tables, form fields, datepickers, autocomplete, dialogs, progress bars). |
| `@angular/cdk` | `^21.2.14` | Component Development Kit | Provides accessible primitives, overlays, scrolling listeners, and autocomplete positioning. |
| `@angular/core` | `^21.0.0` | Core Framework | Angular framework engine, Signals reactivity (`signal`, `computed`), Dependency Injection. |
| `@angular/common` | `^21.0.0` | Framework Common | Directives, pipes (e.g., `DecimalPipe`, `DatePipe`), HTTP client module. |
| `@angular/forms` | `^21.0.0` | Form Management | Reactive forms architecture (`FormControl`, `ReactiveFormsModule`, value change listeners). |
| `@angular/animations` | `^21.2.25` | UI Transitions | Browser animation engine for Angular Material overlays and dialog transitions. |
| `@angular/platform-browser` | `^21.0.0` | Platform | DOM rendering, security sanitization (`DomSanitizer`). |
| `@angular/router` | `^21.0.0` | Routing | Angular navigation and route management. |
| `rxjs` | `~7.8.0` | Reactive Programming | Asynchronous stream operators (`debounceTime`, `distinctUntilChanged`, `switchMap`, `tap`). |
| `tslib` | `^2.3.0` | TypeScript Helpers | Runtime library containing helper functions for TypeScript compilation. |
| `typescript` | `~5.9.2` | Language Compiler | Strongly-typed JavaScript superset compiler. |

#### C. Fallback Client CDN Libraries (`Simple/index.html`)

| Library Name | Version | Delivery | Purpose |
|---|---|---|---|
| **jQuery** | `3.7.1` | CDN (code.jquery.com) | DOM traversal, event handling, and Ajax requests. |
| **Bootstrap 5** | `5.3.3` | CDN (jsdelivr.net) | Responsive grid layout, modals, toasts, and utility classes. |
| **Select2** | `4.1.0-rc.0` | CDN (jsdelivr.net) | Enhanced select dropdown with server-side Ajax autocomplete. |
| **FontAwesome** | `6.5.1` | CDN (cdnjs.cloudflare.com) | Vector icons for financial indicators, arrows, and action buttons. |

---

## 3. Database Schema & Data Models

### 3.1 Entities

#### 1. `Balance` (Accounts Table)
Represents customer/vendor chart of accounts.
- `BalanceId` (`int`, Primary Key): Unique identifier for the account.
- `BalanceName` (`nvarchar(200)`): Account display name (e.g., "Clients Class A").
- `BalanceNameEn` (`nvarchar(200)`): English name.
- `BalanceType` (`char(1)`): Defines accounting classification:
  - `'D'` or `'Debit'` = Debit Account (مدين)
  - `'C'` or `'Credit'` = Credit Account (دائن)
- `ParentCode` (`int`): Code used for concatenated display `(ParentCode - BalanceName)`.
- Multi-tier hierarchy columns: `ParentId`, `Sub1Id` .. `Sub7Id`, `Class`.

#### 2. `BalanceHistory` (Transactions Table)
Stores individual ledger entries and financial movements.
- `BalanceHisId` (`bigint`, Primary Key): Transaction sequence ID.
- `BalanceId` (`int`, Foreign Key): Associated balance account.
- `Date` (`datetime`): Transaction posting date.
- `OrderBill` (`varchar(50)`): Bill, invoice, or voucher reference number.
- `Kind` (`varchar(50)`): Transaction type/classification code.
- `Debtor` (`decimal(18,2)`): Debit amount (مبلغ المدين).
- `Creditor` (`decimal(18,2)`): Credit amount (مبلغ الدائن).
- `PrevBalnce` (`decimal(18,2)`): Running balance prior to this transaction.
- `NewBalnce` (`decimal(18,2)`): Resulting balance recorded by core system.
- `Remarks` (`nvarchar(max)`): Detailed transaction notes; may contain formatted HTML content.
- `ReferenceNo` (`varchar(100)`): External check or reference tracking number.
- `RequestNo`, `OrgId`, `CostCenterId`, `BranchId`: Multi-branch auditing fields.

#### 3. `FileProcess` (Asynchronous Report Tracking)
Persists the lifecycle of background report generation jobs.
- `Id` (`uniqueidentifier` / `Guid`, PK): Unique job tracking ID (`ProcessId`).
- `ReportType` (`int` / Enum `ReportType`): `1 = AccountStatement`.
- `ReportFormat` (`int` / Enum `ReportFormat`): `1 = Excel`, `2 = Pdf`.
- `ProcessStatus` (`int` / Enum `ProcessStatus`):
  - `1 = Pending`: Queued in Channel.
  - `2 = Processing`: Worker actively streaming data or writing file.
  - `3 = Completed`: File generated and ready on disk.
  - `4 = Failed`: Error encountered during generation.
- `Parameters` (`nvarchar(max)`): JSON-serialized filter parameters (`balanceId`, `fromDate`, `toDate`).
- `FilePath` (`nvarchar(500)`): Absolute on-disk storage location for the completed artifact.
- `ConnectionId` (`nvarchar(100)`): SignalR client connection identifier for targeted notifications.
- `CreatedAt`, `StartedAt`, `CompletedAt`: Audit timestamps.
- `ErrorMessage` (`nvarchar(max)`): Stack trace/message if generation fails.

---

## 4. Financial Calculations & Business Logic

The system strictly enforces double-entry accounting rules across both summary widgets and transaction rows:

### 4.1 Summary KPIs (Stored Procedure: `dbo.sp_GetBalanceHistorySummary`)

To avoid calculating entire historical datasets in the frontend or transferring tens of thousands of rows over the network, calculations are executed server-side via the stored procedure:

```sql
EXEC dbo.sp_GetBalanceHistorySummary 
     @BalanceId = @balanceId, 
     @FromDate = @fromDate, 
     @ToDate = @toDate
```

The Stored Procedure computes and returns:
1. **First Previous Balance (`FirstPreviousBalance`):** The `PrevBalnce` of the chronologically earliest transaction within the selected date range.
2. **Total Debits (`TotalDebits`):** Aggregate sum of the `Debtor` column:
   $$\text{Total Debits} = \sum \text{Debtor}$$
3. **Total Credits (`TotalCredits`):** Aggregate sum of the `Creditor` column:
   $$\text{Total Credits} = \sum \text{Creditor}$$
4. **Final Balance (`FinalBalance`):** Computed dynamically based on the account's classification (`BalanceType`):
   - **For Debit Accounts (`BalanceType = 'D'`):**
     $$\text{Final Balance} = (\text{First Previous Balance} + \text{Total Debits}) - \text{Total Credits}$$
   - **For Credit Accounts (`BalanceType = 'C'`):**
     $$\text{Final Balance} = (\text{First Previous Balance} + \text{Total Credits}) - \text{Total Debits}$$

### 4.2 Row-Level Running Final Balance Calculation

On individual grid rows and export tables:
- **Debit Account:**
  $$\text{Row Final Balance} = (\text{PrevBalnce} + \text{Debtor}) - \text{Creditor}$$
- **Credit Account:**
  $$\text{Row Final Balance} = (\text{PrevBalnce} + \text{Creditor}) - \text{Debtor}$$

### 4.3 Database Index Optimization (`IX_BalanceHistory_Balance_Date`)

To support rapid filtering, pagination, and chronological sorting across large historical ledgers without table scans:
- **Script File:** [`EBSSTask.Infrastructure/Persistence/Scripts/IX_BalanceHistory_Balance_Date.sql`](./EBSSTask.Infrastructure/Persistence/Scripts/IX_BalanceHistory_Balance_Date.sql)
- **Definition:**
  ```sql
  CREATE NONCLUSTERED INDEX [IX_BalanceHistory_Balance_Date] ON [dbo].[Balance_History]
  (
      [Balance_ID] ASC,
      [Date] ASC,
      [Balance_His_ID] ASC
  )
  INCLUDE([debtor], [creditor], [Prev_Balnce]) ON [PRIMARY];
  ```
- **Architectural Benefits:**
  - **Index Seek:** B-Tree seek on `(Balance_ID, Date)` immediately restricts search space to only matching transactions.
  - **Deterministic Sort:** Having `Balance_His_ID` in the key ensures immediate retrieval for chronological order without memory sort spills.
  - **Covering Columns (`INCLUDE`):** Including `debtor`, `creditor`, and `Prev_Balnce` satisfies running balance formulas and summary totals directly from index leaf nodes without key lookups.

### 4.4 File Processing & Export Tracking Table (`dbo.FileProcess`)

Asynchronous background report generation jobs, file system paths, processing states, and user SignalR connection identifiers are stored in `dbo.FileProcess`:
- **Script File:** [`EBSSTask.Infrastructure/Persistence/Scripts/CreateTable_FileProcess.sql`](./EBSSTask.Infrastructure/Persistence/Scripts/CreateTable_FileProcess.sql)
- **Schema Details:** Primary Key `Id` (`uniqueidentifier`), `ProcessStatus` (`int`), `ReportType` (`int`), `ReportFormat` (`int`), `Parameters` (`nvarchar(max)`), `CreatedAt`, `StartedAt`, `CompletedAt`, `FilePath`, `UserId`, `ConnectionId`, `ErrorMessage`.

---

## 5. Server-Side Filtering & Full-Text Search

All transaction queries pass through optimized IQueryable extension methods in `QueryFilterExtensions.cs`:

### 5.1 Parameter Filtering (`ApplyFilter`)
```csharp
public static IQueryable<BalanceHistory> ApplyFilter(
    this IQueryable<BalanceHistory> query,
    int? balanceId,
    DateTime? fromDate,
    DateTime? toDate)
{
    if (balanceId.HasValue)
        query = query.Where(h => h.BalanceId == balanceId.Value);

    if (fromDate.HasValue)
        query = query.Where(h => h.Date >= fromDate.Value);

    if (toDate.HasValue)
        query = query.Where(h => h.Date <= toDate.Value);

    return query;
}
```

### 5.2 Server-Side Search Across Grid Columns (`ApplySearch`)
When the user types into the grid search input, the query parameter `Search` is evaluated against 5 key transaction attributes:
```csharp
public static IQueryable<BalanceHistory> ApplySearch(
    this IQueryable<BalanceHistory> query, 
    string? searchTerm)
{
    if (string.IsNullOrWhiteSpace(searchTerm))
        return query;

    var term = searchTerm.Trim();

    return query.Where(h =>
        (h.Remarks != null && EF.Functions.Like(h.Remarks, $"%{term}%")) ||
        (h.ReferenceNo != null && EF.Functions.Like(h.ReferenceNo, $"%{term}%")) ||
        (h.OrderBill != null && EF.Functions.Like(h.OrderBill, $"%{term}%")) ||
        (h.RequestNo != null && EF.Functions.Like(h.RequestNo.ToString(), $"%{term}%")) ||
        EF.Functions.Like(h.BalanceHisId.ToString(), $"%{term}%")
    );
}
```

---

## 6. Asynchronous Background Report Pipeline ("Export all time span statement")

When a user requests a complete statement report covering years of data, the system offloads the work to an asynchronous producer-consumer channel pipeline:

```
[Client]
   │ POST /api/Reports/request/pdf (or excel)
   ▼
[ReportsController] ──────────────────────────┐
   │ Creates FileProcess record (Pending)     │
   ▼                                          ▼
[ChannelReportQueue]                     [FileProcess DB Table]
   │ Writes ReportJob to Channel              │
   ▼                                          │
[ReportGenerationBackgroundService]           │
   │ Picks up job asynchronously              │ Updates status:
   │ Fetches entire filtered dataset          │ Processing -> Completed
   │ Executes ClosedXml / QuestPdf Writer     │
   │ Saves file to disk storage               │
   ▼                                          │
[SignalRReportNotificationService]            │
   │ Broadcasts via ReportHub                 │
   ▼                                          ▼
[Client (Angular/Simple)] ◄───────────────────┘
   │ Receives `OnFileReady` WebSocket event
   │ Automatically triggers browser download
```

### 6.1 Bounded/Unbounded Channel Queue
Implemented via `System.Threading.Channels.Channel<ReportJob>`:
- Thread-safe, non-blocking asynchronous queueing.
- Decouples HTTP request execution from heavy CPU/memory report generation.

### 6.2 `ReportGenerationBackgroundService`
- Inherits from `Microsoft.Extensions.Hosting.BackgroundService`.
- **Concurrency Limiting:** Enforces `SemaphoreSlim(2)` to protect system memory under heavy report generation loads.
- **Fail-Safe Startup Recovery:** On application boot, queries `FileProcess` for any jobs abandoned in `Pending` or `Processing` states (e.g., from server reboot) and marks them with informative timeout errors.

### 6.3 SignalR Real-Time Notification (`ReportHub`)
- Replaces polling mechanisms with direct push notifications.
- Clients subscribe to their process group: `SubscribeToProcess(processId)`.
- Upon file write completion, the server invokes:
  ```csharp
  await Clients.Client(connectionId).SendAsync("OnFileReady", new {
      ProcessId = job.ProcessId,
      FileName = Path.GetFileName(filePath),
      DownloadUrl = $"/api/Files/download/{job.ProcessId}"
  });
  ```

---

## 7. Synchronous Direct Grid Export ("Export grid")

In contrast to the background queue, users can immediately export the exact single page of records currently visible in the grid.

### 7.1 Architecture & Separation of Concerns
- **Zero Database Writes:** Bypasses `FileProcess` and `ChannelReportQueue` completely.
- **In-Memory Generation:** Directly streams `byte[]` in milliseconds.
- **Exact Data Consistency:** Uses the identical `GetBalanceHistoriesAsync` query parameters (`balanceId`, `fromDate`, `toDate`, `search`, `pageNumber`, `pageSize`).

### 7.2 Controller Implementation
```csharp
[HttpGet("export-direct/pdf")]
public async Task<IActionResult> ExportDirectPdf(
    [FromQuery] BalanceHistoryQueryParameters parameters, 
    CancellationToken cancellationToken)
{
    var result = await _balanceHistoryService.GetBalanceHistoriesAsync(parameters, cancellationToken);
    var fileBytes = _directExportService.GenerateDirectPdf(result.Items, parameters);
    return File(fileBytes, "application/pdf", $"Statement_{DateTime.Now:yyyyMMddHHmmss}.pdf");
}

[HttpGet("export-direct/excel")]
public async Task<IActionResult> ExportDirectExcel(
    [FromQuery] BalanceHistoryQueryParameters parameters, 
    CancellationToken cancellationToken)
{
    var result = await _balanceHistoryService.GetBalanceHistoriesAsync(parameters, cancellationToken);
    var fileBytes = _directExportService.GenerateDirectExcel(result.Items, parameters);
    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Statement_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
}
```

### 7.3 Direct Export Specifications
- **PDF Layout:** A4 Portrait (`page.Size(PageSizes.A4)`), custom headers with filter metadata, clean borders, accounting numeric alignments, and page-level total sums.
- **Excel Layout:** ClosedXML workbook with styled headers, custom number formatting (`#,##0.00`), zebra striping, and auto-adjusted column widths.
- **Excluded Columns:** Internal audit fields (`Remarks` and `ReferenceNo`) and KPI cards are excluded to maintain a crisp, readable single-page financial statement.

---

## 8. Frontend Engineering (Angular 19 & Simple Client)

### 8.1 State Management via Angular Signals
The Angular client utilizes fine-grained signals (`signal()`, `computed()`) for reactive rendering without unnecessary change-detection cycles:
- `selectedAccount = signal<BalanceDto | null>(null)`
- `statementItems = signal<BalanceHistoryDto[]>([])`
- `firstPreviousBalance = signal<number>(0)`
- `totalDebits = signal<number>(0)`
- `totalCredits = signal<number>(0)`
- `finalBalance = signal<number>(0)`
- `pageSumDebtor = computed(() => ...)`
- `pageSumCreditor = computed(() => ...)`

### 8.2 Autocomplete with Infinite Scroll & Formatted Display
- Account selection input dynamically searches via `debounceTime(300ms)`.
- Displays options in the standardized format:
  $$\text{(Account Code - Account Name)} \quad \text{e.g. } (11038\text{ - Clients Class A})$$
- Attaches an infinite-scroll listener on the MatAutocomplete panel (`scroll` event) to fetch additional pages seamlessly when scrolling near the bottom.

### 8.3 Global HTTP Loading Interceptor
Located in `loading.interceptor.ts`:
- Intercepts all outgoing HTTP requests and increments an active request counter.
- Drives top progress bars and button loading spinners.
- Decrements safely via RxJS `finalize()` on both success and error responses.

### 8.4 Quick Date Range Presets
Pill buttons provide one-click date range configurations:
1. **Last Month:** 1st day to last day of previous calendar month.
2. **Last 3 Months:** Current date minus 90 days up to today.
3. **Current Year:** January 1st of current year up to today.
4. **Since 2000:** January 1, 2000 up to today.

### 8.5 Safe HTML Remarks Sanitization & Modal Inspection
Transactions with rich HTML in the `Remarks` field:
- Handled safely via DOM sanitization to prevent XSS vulnerabilities.
- Can be viewed in a dedicated `TransactionDetailsDialogComponent` with raw vs. rendered views.

### 8.6 Infinite Scrolling, Paged Chunks & Low-Memory Data Projection
To explore full-span transaction ledgers containing tens of thousands of records without memory bloat, a dedicated **Infinite Scrolling Ledger** was engineered:

1. **Paged Chunks Streaming:**
   - Data is fetched in discrete batches of 50 items (`CHUNK_SIZE = 50`) from `GET /api/BalanceHistories?pageNumber=X&pageSize=50`.
   - As the user scrolls near the bottom of the current chunk, the next chunk is automatically fetched and appended.

2. **Data Projection (Client-Side Heap Optimization):**
   - Full DTOs carry over 18 properties (many strings, timestamps, foreign keys). Storing 50,000 full DTOs causes major V8 JavaScript heap pressure.
   - Incoming DTOs are mapped via `projectBalanceHistoryRow` into `ProjectedBalanceHistoryRow`:
     ```typescript
     export interface ProjectedBalanceHistoryRow {
       id: number;
       date?: string;
       orderBill?: string;
       kind?: string;
       prev: number;
       debit: number;
       credit: number;
       final: number;
     }
     ```
   - All unused fields (`remarks`, `referenceNo`, `insertDate`, `este7kakDate`, `orgId`, `branchId`, `costCenterId`, `billArrange`, `requestNo`) are discarded, saving **~80% RAM per record**.
   - If a user clicks to view transaction details, the full record is fetched on demand by ID (`GET /api/BalanceHistories/{id}`).

3. **Angular CDK Virtual Scrolling ($O(1)$ DOM Elements):**
   - Implemented via `@angular/cdk/scrolling` (`<cdk-virtual-scroll-viewport itemSize="48">`).
   - Rather than rendering 50,000 `<tr>` elements in the browser DOM (which causes severe jank and browser crashes), the virtual scroll viewport creates only **~25 to 30 DOM elements** at any one time.
   - As the user scrolls, existing DOM elements are recycled smoothly at 60fps.

### 8.7 Tab State Management & Data Preservation
- Users can switch seamlessly between **Standard Paginated Grid** and **Full Time-Span (Infinite Scroll)** via top navigation tabs.
- **In-Memory Data Preservation:** When switching between tabs, already-fetched data is preserved in memory and not re-fetched.
- **Controlled Invalidation:** Datasets are only cleared and refreshed when the user changes search criteria (e.g., clicking Search, selecting a new date range, or entering a table search term).

---

## 9. API Endpoints Reference Table

| Method | Endpoint | Description | Query / Body Parameters | Response |
|---|---|---|---|---|
| `GET` | `/api/Balances` | Paginated account list with autocomplete search | `Search`, `PageNumber`, `PageSize` | `PagedResult<BalanceDto>` |
| `GET` | `/api/Balances/{id}` | Account metadata by ID | `id: int` | `BalanceDto` |
| `GET` | `/api/BalanceHistories` | Paginated transaction ledger | `BalanceId`, `FromDate`, `ToDate`, `Search`, `PageNumber`, `PageSize` | `PagedResult<BalanceHistoryDto>` |
| `GET` | `/api/BalanceHistories/{id}` | Single transaction details | `id: long` | `BalanceHistoryDto` |
| `GET` | `/api/BalanceHistories/summary` | SP calculation summary | `balanceId`, `fromDate`, `toDate` | `ReportSummaryData` |
| `GET` | `/api/BalanceHistories/export-direct/pdf` | Synchronous direct PDF export | `BalanceId`, `FromDate`, `ToDate`, `Search`, `PageNumber`, `PageSize` | `File (application/pdf)` |
| `GET` | `/api/BalanceHistories/export-direct/excel` | Synchronous direct Excel export | `BalanceId`, `FromDate`, `ToDate`, `Search`, `PageNumber`, `PageSize` | `File (application/vnd.openxml...)` |
| `POST` | `/api/Reports/request/pdf` | Asynchronous background PDF generation | JSON: `{ balanceId, fromDate, toDate, connectionId }` | `{ processId, status, message }` |
| `POST` | `/api/Reports/request/excel` | Asynchronous background Excel generation | JSON: `{ balanceId, fromDate, toDate, connectionId }` | `{ processId, status, message }` |
| `GET` | `/api/Files` | List of generated background report files | `pageNumber`, `pageSize` | `PagedResult<FileProcessDto>` |
| `GET` | `/api/Files/download/{id}` | Download generated report artifact | `id: Guid` | File Stream |

---

## 10. Execution & Build Instructions

### Option A: Unified Run via Visual Studio / IIS Express (Hosted SPA)
The Angular application is pre-built directly inside `EBSSTask.API/wwwroot` using the development profile (`apiUrl: https://localhost:44323`).

1. Open `EBSSTask.slnx` in **Visual Studio 2022 / 2025**.
2. Run with the **`IIS Express`** profile (`https://localhost:44323`).
3. Access `https://localhost:44323/` to view the hosted Angular SPA, API endpoints, and Swagger UI simultaneously.

### Option B: Standalone Angular Dev Server (HMR)
```bash
# Terminal 1: Run Backend (.NET 10 API)
dotnet run --project "EBSSTask.API/EBSSTask.API.csproj" --launch-profile "IIS Express"

# Terminal 2: Run Frontend (Angular)
cd EBSS.FrontEnd/ebss-angular
npm install
npm start
```
The development server will launch at **`http://localhost:4200`** with Hot Module Replacement (`HMR`).

### Rebuilding Angular into `wwwroot`
To recompile frontend code and deploy fresh bundles directly to the backend's `wwwroot`:
```bash
cd EBSS.FrontEnd/ebss-angular
npm run build:wwwroot
```

### Fallback Client (Simple HTML/JS)
Located at `EBSS.FrontEnd/Simple/index.html`. Can be served using any static web server (VS Code Live Server, IIS, or Python `http.server`).
