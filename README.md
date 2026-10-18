# EBSS Account Statement & Financial Reporting System

An enterprise-grade financial ledger and statement reporting solution built with **.NET 10 (Clean Architecture)** and **Angular (Standalone & Material)**.

---

## 🎯 Technical Scope & Interview Task Disclaimer

> **Note for Technical Reviewers & Hiring Team:**
> This repository was built as an **Interview Technical Assessment**. The implementation strategy deliberately prioritizes:
>
> 1. **Core Domain & Architectural Integrity**: Clean Architecture (Domain, Application, Infrastructure, API) with strict dependency inversion, CQRS-style handlers, FluentValidation pipeline behaviors, and EF Core performance patterns (`AsNoTracking`, projection, stored procedure hybrid execution).
> 2. **Financial Arithmetic & Domain Correctness**: Precise ledger calculations where Final Balance dynamically respects account type (Debit vs. Credit accounts), pagination balance continuity, and exact aggregate summaries.
> 3. **Asynchronous Background Processing & WebSockets**: Decoupled report generation (QuestPDF & ClosedXML) via background queues, returning `202 Accepted` immediately, and notifying the client in real-time over **SignalR WebSockets**.
> 4. **High-Performance Memory Engineering**: 
>    - **Direct Memory Streaming**: Immediate export of active views without disk writes.
>    - **Data Projection & Virtual Scrolling**: Client-side memory footprint reduction by ~80% using projected lightweight models and Angular CDK Virtual Scrolling ($O(1)$ DOM nodes for tens of thousands of rows).
>
> **Intentional Trade-offs**: To focus available time on architectural depth, memory optimization, and domain logic, boilerplate production features (such as OAuth2/JWT authentication, distributed Redis queues, API rate-limiting, and Kubernetes manifests) were intentionally bypassed in favor of demonstrating senior-level full-stack capabilities.

---

## 📋 Prerequisites

Before running the application, ensure the following are installed:
- **.NET 10 SDK** (or .NET 9+ with C# 13/14 preview features enabled).
- **Node.js**: v20.x or v22.x LTS (`npm` v10+).
- **Microsoft SQL Server**: 2019 / 2022 / Express / LocalDB.
- **Angular CLI**: `npm install -g @angular/cli` (Optional, as `npx` or local npm scripts can be used).

---

## 🗄️ Database Setup & Connection String Configuration

The application requires access to the evaluation database (e.g. `elite_erp_23_4_2024` or equivalent schema containing `Balances` and `BalanceHistories` tables).

### 1. Configure the Connection String
Open `EBSSTask.API/appsettings.json` and adjust the `ConnectionStrings:DefaultConnection` property according to your local SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=elite_erp_23_4_2024;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

#### Common Connection String Examples:
- **Local SQL Server with Windows Authentication (Default)**:
  ```json
  "DefaultConnection": "Server=.;Database=elite_erp_23_4_2024;Trusted_Connection=True;TrustServerCertificate=True;"
  ```
- **SQL Server with Username & Password (SQL Auth)**:
  ```json
  "DefaultConnection": "Server=localhost;Database=elite_erp_23_4_2024;User Id=sa;Password=YourSecurePassword;TrustServerCertificate=True;"
  ```
- **SQL Server Express Instance**:
  ```json
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=elite_erp_23_4_2024;Trusted_Connection=True;TrustServerCertificate=True;"
  ```
- **LocalDB**:
  ```json
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=elite_erp_23_4_2024;Trusted_Connection=True;TrustServerCertificate=True;"
  ```

### 2. Database Scripts & Automatic Schema Initialization

The application includes an **Automated Database Initialization Service** (`DatabaseInitializer.cs`) that runs at application startup:
1. **Connectivity Check:** Validates whether the database connection (`CanConnectAsync`) is accessible via the configured connection string.
2. **Automatic Script Application:** If connection is valid, it automatically applies the schema scripts in sequence:
   - **Table:** Creates `dbo.FileProcess` if it doesn't already exist.
   - **Stored Procedure:** Creates or updates `dbo.sp_GetBalanceHistorySummary` using `CREATE OR ALTER`.
   - **Index:** Creates `IX_BalanceHistory_Balance_Date` if it doesn't already exist.
3. **Safe Idempotency:** If the table or index already exists, the script does nothing.

You may also inspect or run the SQL scripts manually from `EBSSTask.Infrastructure/Persistence/Scripts/`:

#### A. Background Report File Processing Table (`dbo.FileProcess`)
Stores asynchronous report generation requests, file paths, and processing statuses tracked by the background worker and SignalR hub.
- **Script Location:** [`EBSSTask.Infrastructure/Persistence/Scripts/CreateTable_FileProcess.sql`](./EBSSTask.Infrastructure/Persistence/Scripts/CreateTable_FileProcess.sql)
- **Safe Execution:** Uses `IF OBJECT_ID(N'[dbo].[FileProcess]', N'U') IS NULL`.

#### B. Summary Stored Procedure (`dbo.sp_GetBalanceHistorySummary`)
Computes summary KPIs (`First Previous Balance`, `Total Debits`, `Total Credits`, and dynamically calculated `Final Balance` based on Debit/Credit account type) at the database level.
- **Script Location:** [`EBSSTask.Infrastructure/Persistence/Scripts/sp_GetBalanceHistorySummary.sql`](./EBSSTask.Infrastructure/Persistence/Scripts/sp_GetBalanceHistorySummary.sql)
- **Safe Execution:** Uses `CREATE OR ALTER PROCEDURE dbo.sp_GetBalanceHistorySummary`.

#### C. Performance Index (`IX_BalanceHistory_Balance_Date`)
Accelerates statement queries, date filtering, and chronological sorting across large historical ledgers with zero key lookups for balance calculation:
- **Script Location:** [`EBSSTask.Infrastructure/Persistence/Scripts/IX_BalanceHistory_Balance_Date.sql`](./EBSSTask.Infrastructure/Persistence/Scripts/IX_BalanceHistory_Balance_Date.sql)
- **Index Definition:**
  ```sql
  CREATE NONCLUSTERED INDEX [IX_BalanceHistory_Balance_Date] ON [dbo].[Balance_History]
  (
      [Balance_ID] ASC,
      [Date] ASC,
      [Balance_His_ID] ASC
  )
  INCLUDE([debtor], [creditor], [Prev_Balnce])ON [PRIMARY];
  ```
- **Performance Impact:** Eliminates full table scans on `Balance_History`, enabling rapid B-Tree seeks for account queries, date range lookups, pagination chunks, and summary aggregations without key lookups for debit/credit/prev balance.

---

## 🚀 How to Run the Application

The solution supports two execution modes:

### Option A: Unified Full-Stack Run via Visual Studio / IIS Express (Recommended)
The Angular application has already been pre-built into `EBSSTask.API/wwwroot` using the development profile (`apiUrl: https://localhost:44323`).

1. Open `EBSSTask.slnx` or `EBSSTask.sln` in **Visual Studio 2022 / 2025**.
2. Set **`EBSSTask.API`** as the Startup Project.
3. Select the **`IIS Express`** profile from the run dropdown (configured for port `https://localhost:44323`).
4. Press **`F5`** or **`Ctrl + F5`**.
5. Your browser will open at **`https://localhost:44323/`**:
   - The Angular UI is served directly from `wwwroot`.
   - API endpoints are active at `https://localhost:44323/api/...`.
   - Swagger documentation is available at `https://localhost:44323/swagger`.
   - Real-time SignalR Hub is listening at `https://localhost:44323/hubs/reports`.

---

### Option B: Standalone Run (Backend + Angular Dev Server with HMR)
If you prefer running the Angular frontend in hot-reload mode (`HMR`):

#### 1. Start the Backend API (.NET 10):
```bash
# From workspace root:
dotnet run --project "EBSSTask.API/EBSSTask.API.csproj" --launch-profile "IIS Express"
```
*(Or use `--launch-profile "https"` if running outside IIS Express)*.

#### 2. Start the Frontend (Angular):
```bash
cd "EBSS.FrontEnd/ebss-angular"
npm install
npm start
```
The development server will launch at **`http://localhost:4200`** with Hot Module Replacement (`HMR`).

---

### Rebuilding Angular into `wwwroot`
If you modify frontend code and want to refresh the files hosted inside the backend `wwwroot`:
```bash
cd "EBSS.FrontEnd/ebss-angular"
npm run build:wwwroot
```
*(This builds the development bundle with `apiUrl: https://localhost:44323` and deploys it to `EBSSTask.API/wwwroot` while preserving generated reports)*.

---

## 🌟 Key Architecture & Feature Highlights

| Area | Feature | Implementation Details |
| :--- | :--- | :--- |
| **Backend Architecture** | Clean Architecture | Strict separation of `Domain`, `Application`, `Infrastructure`, and `API` layers. |
| **Summary Calculations** | Stored Procedure Hybrid | Summary cards (`First Prev`, `Total Debits`, `Total Credits`, `Final Balance`) execute `dbo.sp_GetBalanceHistorySummary` for instantaneous DB-level aggregation. |
| **Direct Exports** | Zero-Disk Memory Streaming | Grid-level PDF and Excel exports stream directly from server RAM to browser in milliseconds. |
| **Asynchronous Export** | Background Queue + SignalR | Full-span exports return `202 Accepted` and generate in the background, pushing real-time notifications when the file is ready. |
| **Infinite Scrolling** | Paged Chunks + Data Projection | Seamless infinite scroll loading 50-record chunks with memory-reduced models discarding unused fields (~80% heap savings). |
| **DOM Optimization** | Angular CDK Virtual Scroll | Constant $O(1)$ DOM element count (~25 items in DOM regardless of total records loaded) ensuring 60fps scrolling. |
| **Tab State Management** | In-Memory Data Preservation | Switching between Paginated Grid and Infinite Scroll preserves loaded data without redundant re-fetching. |

---

## 📚 In-Depth Technical Documentation

For complete technical specifications, architectural diagrams, financial ledger formulas, and API documentation, please refer to:

👉 **[TECHNICAL_DOCUMENTATION.md](./TECHNICAL_DOCUMENTATION.md)**