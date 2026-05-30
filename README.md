# ParaBank RPA Automation

A C#/.NET 8 RPA-style automation solution for processing customer records in ParaBank. The application uploads customer data, registers or logs in each customer, opens a new account, submits a loan request, streams live progress to the web UI, and exports an operator-ready Excel report in USD and EUR.

## Why this project fits an RPA Developer role

- **End-to-end business-process automation:** CSV/Excel input → browser automation → account opening → loan request → Excel output.
- **RPA workflow design:** Each browser operation is split into a dedicated flow under `Rpa/Flows`.
- **Maintainability:** The orchestrator depends on interfaces, not concrete infrastructure classes.
- **Testing:** Unit tests cover data parsing, validation, money conversion, and report generation using small in-memory/temporary fixtures, not the submitted task data.
- **Deployment awareness:** Includes build/test/run commands and clear runtime requirements.
- **Operational resilience:** Uses explicit waits, long timeout support, existing-user login fallback, and live progress logging.

## Project structure

```text
ParaBankAutomation/
├── Abstractions/              # Interfaces for input, reports, currency, validation, RPA factory
├── Controllers/               # MVC/API endpoints
├── Domain/                    # Validation result model
├── Hubs/                      # SignalR progress hub
├── Models/                    # DTOs and business records
├── Rpa/                       # Selenium browser session and page-flow objects
│   └── Flows/                 # Register, login, open account, request loan
├── Services/                  # Orchestration, CSV/XLSX reader, currency, report writer, validation
├── Views/                     # Razor UI markup
├── wwwroot/                   # Static CSS and JavaScript assets
└── ParaBankAutomation.Tests/  # xUnit test project
```

## Requirements

- .NET 8 SDK
- Google Chrome
- Internet access to ParaBank demo site and Frankfurter currency API
- Windows, macOS, or Linux with Chrome available to Selenium Manager

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
```

## Run locally

```bash
cd ParaBankAutomation
dotnet run
```

Then open the shown local URL in your browser, upload the customer CSV/XLSX supplied for the task, and run the automation. No customer data is hardcoded or bundled in the submitted project.

## Runtime configuration

ParaBank can respond slowly. The default Selenium wait timeout is 180 seconds. Override it when needed:

### PowerShell

```powershell
$env:PARABANK_WAIT_SECONDS = "300"
dotnet run
```

### Bash

```bash
export PARABANK_WAIT_SECONDS=300
dotnet run
```

The browser can be launched headless by posting `headless: true` to the run API. The UI currently sends `false` so the automation is easy to observe during review.

## Report output

The generated report contains:

- `Operations` sheet with one row per customer.
- `Summary` sheet with total processed, completed, failed, loan requests submitted, and accounts opened.
- USD and EUR values based on a live USD→EUR rate, with a safe fallback rate if the API is unavailable.

## Notes for reviewers

The validation layer records non-blocking data quality warnings, such as invalid DOB or blank/zero deposit, in the report notes while keeping the browser automation flow running when ParaBank itself can process the record.


## Runtime input model

The application does not include bundled customer data. Customer files are uploaded through the web UI at runtime and are stored temporarily under the operating system temp folder. Unit tests use synthetic generated fixtures only to verify parsing, validation, report generation, and conversion logic.

## UI assets

Razor views contain only the page markup. Shared styling is kept in `ParaBankAutomation/wwwroot/css/site.css`, and dashboard behavior is kept in `ParaBankAutomation/wwwroot/js/dashboard.js`. ASP.NET Core serves these files through `app.UseStaticFiles()` in `Program.cs`.
