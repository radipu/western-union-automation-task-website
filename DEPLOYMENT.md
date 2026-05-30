# Deployment and Review Checklist

## Pre-submission checklist

1. Restore packages.
2. Build the solution.
3. Run the unit tests.
4. Run the app locally.
5. Start the web application and upload the current assessment customer file through the UI.
6. Run the automation once against ParaBank.
7. Download and attach the generated Excel report.

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet run --project ParaBankAutomation/ParaBankAutomation.csproj
```

## Environment variables

| Variable | Default | Purpose |
|---|---:|---|
| `PARABANK_WAIT_SECONDS` | `180` | Selenium explicit wait and page-load timeout. Use `300` for slow ParaBank responses. |

## Troubleshooting

- **ChromeDriver mismatch:** Selenium Manager should resolve the correct driver automatically. Make sure Chrome is installed and updated.
- **ParaBank slow or unavailable:** Increase `PARABANK_WAIT_SECONDS` to `300` or `600` and retry.
- **Currency API unavailable:** The app logs a warning and falls back to `0.92` USD→EUR.
- **Existing usernames:** The automation logs in with the supplied credentials and continues the workflow.

## Customer input file

Do not place the task CSV inside the source tree. Start the web application, upload the current customer file through the UI, and run the automation. The uploaded file is copied to a temporary runtime location only.


## Runtime input model

The application does not include bundled customer data. Customer files are uploaded through the web UI at runtime and are stored temporarily under the operating system temp folder. Unit tests use synthetic generated fixtures only to verify parsing, validation, report generation, and conversion logic.


## Static files

The web UI uses external static assets under `ParaBankAutomation/wwwroot`:

- `wwwroot/css/site.css` for styling
- `wwwroot/js/dashboard.js` for dashboard behavior

`Program.cs` calls `app.UseStaticFiles()`, so these assets are served automatically when the application runs.
