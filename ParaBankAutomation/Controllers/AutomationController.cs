using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ParaBankAutomation.Abstractions;
using ParaBankAutomation.Hubs;
using ParaBankAutomation.Models;
using ParaBankAutomation.Services;

namespace ParaBankAutomation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutomationController : ControllerBase
{
    private readonly AutomationOrchestrator _orchestrator;
    private readonly ICustomerSourceReader _csvReader;
    private readonly IOperationReportWriter _reportWriter;
    private readonly IExchangeRateProvider _currency;
    private readonly IHubContext<ProgressHub> _hub;
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(
        AutomationOrchestrator orchestrator,
        ICustomerSourceReader csvReader,
        IOperationReportWriter reportWriter,
        IExchangeRateProvider currency,
        IHubContext<ProgressHub> hub,
        ILogger<AutomationController> logger)
    {
        _orchestrator = orchestrator;
        _csvReader = csvReader;
        _reportWriter = reportWriter;
        _currency = currency;
        _hub = hub;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file received." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".csv" or ".xlsx" or ".xlsm"))
            return BadRequest(new { message = "Only CSV or Excel files (.csv, .xlsx) are accepted." });

        // Save to a temp path so the orchestrator can re-read it later
        var dir = Path.Combine(Path.GetTempPath(), "ParaBankUploads");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"upload_{DateTime.Now:yyyyMMddHHmmss}{ext}");

        await using (var fs = System.IO.File.Create(path))
            await file.CopyToAsync(fs);

        HomeController.UploadedFilePath = path;

        var customers = _csvReader.ReadCustomers(path);

        return Ok(new
        {
            count = customers.Count,
            customers = customers.Select(c => new
            {
                rowNumber = c.RowNumber,
                fullName = c.FullName,
                username = c.Username,
                accountType = c.AccountType,
                initialDeposit = c.InitialDepositUsd,
                downPayment = c.DownPayment,
                dob = c.DobRaw,
                debitCard = c.DebitCardNumber,
                cvv = c.Cvv
            })
        });
    }

    [HttpPost("run")]
    public IActionResult Run([FromBody] RunRequest? req)
    {
        if (HomeController.CurrentSession?.IsRunning == true)
            return Conflict(new { message = "An automation run is already in progress." });

        var filePath = HomeController.UploadedFilePath;
        if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
            return BadRequest(new { message = "No customer file found. Please upload a CSV or Excel file before running the automation." });

        var session = new AutomationSession { IsRunning = true, StartedAt = DateTime.UtcNow };
        HomeController.CurrentSession = session;

        var settings = new AutomationSettings
        {
            RunBrowserHeadless = req?.Headless ?? false
        };

        _ = Task.Run(async () =>
        {
            try
            {
                var progress = new Progress<AutomationProgress>(async p =>
                {
                    session.Log.Add($"[{DateTime.Now:HH:mm:ss}] {p.Message}");
                    await _hub.Clients.All.SendAsync("ProgressUpdate", new
                    {
                        message = p.Message,
                        completed = p.Completed,
                        total = p.Total,
                        username = p.Username,
                        status = p.Status
                    });
                });

                await _orchestrator.RunAsync(filePath, settings, session, progress, CancellationToken.None);
            }
            catch (Exception ex)
            {
                session.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Automation session failed.");
            }
            finally
            {
                session.IsRunning = false;
                session.CompletedAt = DateTime.UtcNow;
                await _hub.Clients.All.SendAsync("SessionComplete", new
                {
                    success = session.SuccessCount,
                    failed = session.FailedCount,
                    total = session.TotalCustomers
                });
            }
        });

        return Ok(new { message = "Automation started." });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        var s = HomeController.CurrentSession;
        if (s is null) return Ok(new { hasSession = false });

        return Ok(new
        {
            hasSession = true,
            isRunning = s.IsRunning,
            total = s.TotalCustomers,
            completed = s.CompletedCount,
            success = s.SuccessCount,
            failed = s.FailedCount,
            hasReport = !string.IsNullOrWhiteSpace(s.ReportPath),
            log = s.Log.TakeLast(50),
            results = s.Results.Select(r => new
            {
                name = r.CustomerName,
                username = r.Username,
                accountType = r.AccountType,
                account = r.OpenedAccountNumber,
                loanRequested = r.LoanRequested,
                loanStatus = r.LoanStatus,
                loanUsd = r.LoanAmountUsd,
                loanEur = r.LoanAmountEur,
                downUsd = r.DownPaymentUsd,
                downEur = r.DownPaymentEur,
                status = r.AutomationStatus,
                notes = r.Notes,
                processedAt = r.ProcessedAt
            })
        });
    }

    [HttpGet("report")]
    public async Task<IActionResult> DownloadReport()
    {
        var s = HomeController.CurrentSession;

        if (s is null || !s.Results.Any())
            return NotFound(new { message = "No results available yet." });

        var rate = await _currency.GetUsdToEurRateAsync();
        var bytes = _reportWriter.WriteReport(s.Results, rate);
        var name = $"ParaBank_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            name);
    }
}

public sealed class RunRequest
{
    public bool Headless { get; set; }
}
