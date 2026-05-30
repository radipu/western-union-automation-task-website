using ClosedXML.Excel;
using ParaBankAutomation.Abstractions;
using ParaBankAutomation.Models;

namespace ParaBankAutomation.Services;

public sealed class ExcelReportWriter : IOperationReportWriter
{
    public byte[] WriteReport(IReadOnlyList<OperationReportRow> rows, decimal exchangeRate)
    {
        using var wb = new XLWorkbook();
        BuildOperationsSheet(wb, rows, exchangeRate);
        BuildSummarySheet(wb, rows);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void BuildOperationsSheet(
        IXLWorkbook wb, IReadOnlyList<OperationReportRow> rows, decimal rate)
    {
        var ws = wb.Worksheets.Add("Operations");

        // Title
        ws.Cell("A1").Value = "ParaBank Operator Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        ws.Cell("A2").Value = $"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}";
        ws.Cell("A2").Style.Font.Italic = true;

        ws.Cell("A3").Value = $"Exchange Rate: 1 USD = {rate:F4} EUR";
        ws.Cell("A3").Style.Font.Italic = true;

        var headers = new[]
        {
            "Row", "Customer Name", "Username", "Account Type",
            "Initial Deposit USD", "Initial Deposit EUR",
            "Loan Amount USD", "Loan Amount EUR",
            "Down Payment USD", "Down Payment EUR",
            "New Account #", "Loan Requested", "Loan Status",
            "DOB", "Debit Card", "CVV",
            "Automation Status", "Notes", "Processed At"
        };

        for (int col = 0; col < headers.Length; col++)
        {
            var cell = ws.Cell(5, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            int row = 6 + i;

            bool ok = r.AutomationStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase);
            var rowColour = ok ? XLColor.FromHtml("#d4edda") : XLColor.FromHtml("#f8d7da");
            if (ok && i % 2 == 1) rowColour = XLColor.FromHtml("#f0f7ff");

            ws.Cell(row, 1).Value = r.RowNumber;
            ws.Cell(row, 2).Value = r.CustomerName;
            ws.Cell(row, 3).Value = r.Username;
            ws.Cell(row, 4).Value = r.AccountType;
            ws.Cell(row, 5).Value = r.InitialDepositUsd;
            ws.Cell(row, 6).Value = r.InitialDepositEur;
            ws.Cell(row, 7).Value = r.LoanAmountUsd;
            ws.Cell(row, 8).Value = r.LoanAmountEur;
            ws.Cell(row, 9).Value = r.DownPaymentUsd;
            ws.Cell(row, 10).Value = r.DownPaymentEur;
            ws.Cell(row, 11).Value = r.OpenedAccountNumber;
            ws.Cell(row, 12).Value = r.LoanRequested;
            ws.Cell(row, 13).Value = r.LoanStatus;
            ws.Cell(row, 14).Value = r.DateOfBirth;
            ws.Cell(row, 15).Value = r.DebitCardNumber;
            ws.Cell(row, 16).Value = r.Cvv;
            ws.Cell(row, 17).Value = r.AutomationStatus;
            ws.Cell(row, 18).Value = r.Notes;
            ws.Cell(row, 19).Value = r.ProcessedAt.ToString("yyyy-MM-dd HH:mm");

            ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 6).Style.NumberFormat.Format = "€#,##0.00";
            ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 8).Style.NumberFormat.Format = "€#,##0.00";
            ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 10).Style.NumberFormat.Format = "€#,##0.00";

            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = rowColour;

            var statusCell = ws.Cell(row, 17);
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Font.FontColor = ok
                ? XLColor.FromHtml("#155724")
                : XLColor.FromHtml("#721c24");
        }

        var used = ws.RangeUsed();
        if (used is not null)
        {
            used.CreateTable();
            ws.SheetView.FreezeRows(5);
        }

        ws.Columns().AdjustToContents();
        ws.Column(13).Width = 40;
        ws.Column(18).Width = 40;
    }

    private static void BuildSummarySheet(IXLWorkbook wb, IReadOnlyList<OperationReportRow> rows)
    {
        var ws = wb.Worksheets.Add("Summary");

        ws.Cell("A1").Value = "ParaBank Automation Summary";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        var data = new (string Label, object Value)[]
        {
            ("Total records processed", rows.Count),
            ("Completed successfully", rows.Count(r => r.AutomationStatus == "Completed")),
            ("Automation / Validation Failed", rows.Count(r => r.AutomationStatus.Contains("Failed"))),
            ("Loan requests submitted", rows.Count(r => r.LoanRequested == "Yes")),
            ("New accounts opened", rows.Count(r =>
                !string.IsNullOrWhiteSpace(r.OpenedAccountNumber) &&
                r.OpenedAccountNumber != "Not opened")),
            ("Report generated at", DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
        };

        for (int i = 0; i < data.Length; i++)
        {
            ws.Cell(3 + i, 1).Value = data[i].Label;
            ws.Cell(3 + i, 1).Style.Font.Bold = true;
            ws.Cell(3 + i, 2).Value = data[i].Value.ToString();
        }

        ws.Columns().AdjustToContents();
    }
}
