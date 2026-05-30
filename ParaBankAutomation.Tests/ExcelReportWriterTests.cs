using ClosedXML.Excel;
using ParaBankAutomation.Models;
using ParaBankAutomation.Services;
using Xunit;

namespace ParaBankAutomation.Tests;

public sealed class ExcelReportWriterTests
{
    [Fact]
    public void WriteReport_CreatesOperationsAndSummarySheets()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var expectedCustomerName = $"Synthetic Customer {unique}";
        var expectedUsername = $"user_{unique}";

        var rows = new[]
        {
            new OperationReportRow
            {
                RowNumber = 2,
                CustomerName = expectedCustomerName,
                Username = expectedUsername,
                AccountType = "Checking",
                InitialDepositUsd = 500m,
                InitialDepositEur = 460m,
                LoanAmountUsd = 10000m,
                LoanAmountEur = 9200m,
                DownPaymentUsd = 100m,
                DownPaymentEur = 92m,
                OpenedAccountNumber = $"ACC-{unique}",
                LoanRequested = "Yes",
                LoanStatus = $"Approved - loan account LOAN-{unique}",
                AutomationStatus = "Completed",
                ProcessedAt = new DateTime(2026, 5, 29, 22, 49, 5)
            }
        };

        var writer = new ExcelReportWriter();
        var bytes = writer.WriteReport(rows, 0.92m);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        Assert.True(workbook.Worksheets.Contains("Operations"));
        Assert.True(workbook.Worksheets.Contains("Summary"));
        Assert.Equal("ParaBank Operator Report", workbook.Worksheet("Operations").Cell("A1").GetString());
        Assert.Equal(expectedCustomerName, workbook.Worksheet("Operations").Cell(6, 2).GetString());
        Assert.Equal("Completed successfully", workbook.Worksheet("Summary").Cell(4, 1).GetString());
        Assert.Equal("1", workbook.Worksheet("Summary").Cell(4, 2).GetString());
    }
}
