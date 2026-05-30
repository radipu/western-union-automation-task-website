namespace ParaBankAutomation.Models;

public sealed class AutomationSettings
{
    public string ParaBankUrl { get; set; } = "https://parabank.parasoft.com/parabank/index.htm";
    public decimal LoanAmountUsd { get; set; } = 10_000m;
    public decimal DownPaymentRate { get; set; } = 0.20m;
    public decimal UsdToEurRate { get; set; } = 0.92m;
    public bool RunBrowserHeadless { get; set; } = false;
}

public sealed class AutomationProgress
{
    public string Message { get; set; }
    public int Completed { get; set; }
    public int Total { get; set; }
    public string? Username { get; set; }
    public string Status { get; set; } = "Running";

    public AutomationProgress(string message, int completed, int total, string? username = null, string status = "Running")
    {
        Message = message;
        Completed = completed;
        Total = total;
        Username = username;
        Status = status;
    }
}

public sealed class AutomationSession
{
    public bool IsRunning { get; set; }
    public List<OperationReportRow> Results { get; set; } = new();
    public List<string> Log { get; set; } = new();
    public int TotalCustomers { get; set; }
    public int CompletedCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ReportPath { get; set; }
    public string? ErrorMessage { get; set; }

    public int SuccessCount => Results.Count(r => r.AutomationStatus == "Completed");
    public int FailedCount => Results.Count(r => r.AutomationStatus.Contains("Failed"));
}
