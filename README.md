# EBSS Account Statement System

An enterprise-grade financial reporting and statement analysis platform built with **.NET 10 (Clean Architecture)** and **Angular 19**.

## Documentation
For complete technical specifications, architectural patterns, database schemas, financial calculation formulas, background queues, and API contracts, please refer to:

👉 **[TECHNICAL_DOCUMENTATION.md](./TECHNICAL_DOCUMENTATION.md)**

## Quick Start

### Backend (.NET 10)
```bash
dotnet build "EBSSTask.slnx"
dotnet run --project "EBSSTask.API/EBSSTask.API.csproj" --launch-profile "https"
```

### Frontend (Angular 19)
```bash
cd EBSS.FrontEnd/ebss-angular
npm install
npx ng serve -o --hmr
```