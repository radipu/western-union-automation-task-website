using ParaBankAutomation.Abstractions;
using ParaBankAutomation.Models;

namespace ParaBankAutomation.Services;

public sealed class AutomationOrchestrator
{
    private readonly ICustomerSourceReader _customerReader;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IOperationReportWriter _reportWriter;
    private readonly ICustomerAutomationServiceFactory _automationFactory;
    private readonly ICustomerValidationService _validator;
    private readonly ILogger<AutomationOrchestrator> _logger;

    public AutomationOrchestrator(
        ICustomerSourceReader customerReader,
        IExchangeRateProvider exchangeRateProvider,
        IOperationReportWriter reportWriter,
        ICustomerAutomationServiceFactory automationFactory,
        ICustomerValidationService validator,
        ILogger<AutomationOrchestrator> logger)
    {
        _customerReader = customerReader;
        _exchangeRateProvider = exchangeRateProvider;
        _reportWriter = reportWriter;
        _automationFactory = automationFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task RunAsync(
        string customerFilePath,
        AutomationSettings settings,
        AutomationSession session,
        IProgress<AutomationProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerFilePath);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(progress);

        settings.UsdToEurRate = await _exchangeRateProvider.GetUsdToEurRateAsync();
        _logger.LogInformation("USD→EUR rate: {Rate}", settings.UsdToEurRate);

        var customers = _customerReader.ReadCustomers(customerFilePath);
        session.TotalCustomers = customers.Count;

        using var automation = _automationFactory.Create();

        for (var i = 0; i < customers.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var customer = customers[i];
            progress.Report(new AutomationProgress(
                $"Processing {customer.FullName}…", i, customers.Count,
                customer.Username, "Running"));

            var validation = _validator.Validate(customer);
            if (!validation.IsValid)
            {
                var failedRow = BuildValidationFailure(customer, settings, validation);
                AddResult(session, progress, failedRow, i, customers.Count, customer.Username);
                continue;
            }

            var row = await ProcessCustomerAsync(
                automation, customer, settings, session, progress, i, customers.Count, cancellationToken);

            if (validation.HasWarnings)
                row.Notes = MergeNotes(row.Notes, validation.ToNoteText());

            AddResult(session, progress, row, i, customers.Count, customer.Username);
        }

        var reportBytes = _reportWriter.WriteReport(session.Results, settings.UsdToEurRate);
        var outputDir = Path.Combine(Path.GetTempPath(), "ParaBankReports");
        Directory.CreateDirectory(outputDir);
        var reportPath = Path.Combine(outputDir, $"ParaBank_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        await File.WriteAllBytesAsync(reportPath, reportBytes, cancellationToken);

        session.ReportPath = reportPath;
    }

    private async Task<OperationReportRow> ProcessCustomerAsync(
        ICustomerAutomationService automation,
        CustomerProfile customer,
        AutomationSettings settings,
        AutomationSession session,
        IProgress<AutomationProgress> progress,
        int currentIndex,
        int totalCustomers,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(() =>
                automation.ProcessCustomer(customer, settings, msg =>
                {
                    _logger.LogInformation(msg);
                    session.Log.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
                    progress.Report(new AutomationProgress(msg, currentIndex, totalCustomers,
                        customer.Username, "Running"));
                }), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for {User}", customer.Username);
            return BuildUnexpectedFailure(customer, ex);
        }
    }

    private static void AddResult(
        AutomationSession session,
        IProgress<AutomationProgress> progress,
        OperationReportRow row,
        int currentIndex,
        int totalCustomers,
        string username)
    {
        session.Results.Add(row);
        session.CompletedCount = currentIndex + 1;

        var doneStatus = row.AutomationStatus == "Completed" ? "Success" : "Failed";
        progress.Report(new AutomationProgress(
            $"Finished {row.CustomerName}: {row.AutomationStatus}",
            currentIndex + 1, totalCustomers, username, doneStatus));
    }

    private static OperationReportRow BuildValidationFailure(
        CustomerProfile customer,
        AutomationSettings settings,
        CustomerValidationResult validation)
    {
        var converter = new MoneyConverter(settings.UsdToEurRate);
        var downPaymentUsd = MoneyConverter.CalculateDownPayment(customer.InitialDepositUsd, settings.DownPaymentRate);

        return new OperationReportRow
        {
            RowNumber = customer.RowNumber,
            CustomerName = customer.FullName,
            Username = customer.Username,
            AccountType = customer.AccountType,
            InitialDepositUsd = customer.InitialDepositUsd,
            InitialDepositEur = converter.ToEur(customer.InitialDepositUsd),
            LoanAmountUsd = settings.LoanAmountUsd,
            LoanAmountEur = converter.ToEur(settings.LoanAmountUsd),
            DownPaymentUsd = downPaymentUsd,
            DownPaymentEur = converter.ToEur(downPaymentUsd),
            OpenedAccountNumber = "Not opened",
            LoanRequested = "No",
            LoanStatus = "Not requested",
            DateOfBirth = customer.DateOfBirth?.ToString("yyyy-MM-dd") ?? customer.DobRaw,
            DebitCardNumber = customer.DebitCardNumber,
            Cvv = customer.Cvv,
            AutomationStatus = "Validation Failed",
            Notes = validation.ToNoteText()
        };
    }

    private static OperationReportRow BuildUnexpectedFailure(CustomerProfile customer, Exception ex) =>
        new()
        {
            RowNumber = customer.RowNumber,
            CustomerName = customer.FullName,
            Username = customer.Username,
            AccountType = customer.AccountType,
            AutomationStatus = "Automation Failed",
            Notes = ex.Message,
            LoanRequested = "No",
            LoanStatus = "Not requested",
            OpenedAccountNumber = "Not opened"
        };

    private static string MergeNotes(string existingNotes, string additionalNotes)
    {
        if (string.IsNullOrWhiteSpace(existingNotes)) return additionalNotes;
        if (string.IsNullOrWhiteSpace(additionalNotes)) return existingNotes;
        return $"{existingNotes} | {additionalNotes}";
    }
}
