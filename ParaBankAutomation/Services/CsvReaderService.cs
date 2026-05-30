using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ParaBankAutomation.Abstractions;
using ParaBankAutomation.Models;

namespace ParaBankAutomation.Services;

public sealed class CsvReaderService : ICustomerSourceReader
{
    private readonly ILogger<CsvReaderService> _logger;

    public CsvReaderService(ILogger<CsvReaderService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<CustomerProfile> ReadCustomers(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Customer file not found.", filePath);

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        var customers = ext is ".xlsx" or ".xlsm"
            ? ReadExcel(filePath)
            : ReadCsv(filePath);

        _logger.LogInformation("Loaded {Count} customer(s) from {File}.", customers.Count, Path.GetFileName(filePath));
        return customers;
    }

    private static List<CustomerProfile> ReadCsv(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        var result = new List<CustomerProfile>();
        var row = 2;

        while (csv.Read())
        {
            result.Add(MapCustomer(row, col => SafeGet(csv, col)));
            row++;
        }

        return result;
    }

    private static string SafeGet(CsvReader csv, string col)
    {
        try { return csv.GetField(col)?.Trim() ?? string.Empty; }
        catch { return string.Empty; }
    }

    private static List<CustomerProfile> ReadExcel(string filePath)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        var headerRow = ws.FirstRowUsed()
            ?? throw new InvalidOperationException("The workbook is empty.");

        var headers = headerRow.CellsUsed()
            .Where(c => !string.IsNullOrWhiteSpace(c.GetString()))
            .GroupBy(c => Normalise(c.GetString()))
            .ToDictionary(g => g.Key, g => g.First().Address.ColumnNumber);

        var result = new List<CustomerProfile>();
        var firstData = headerRow.RowNumber() + 1;
        var lastData = ws.LastRowUsed()?.RowNumber() ?? firstData - 1;

        for (int r = firstData; r <= lastData; r++)
        {
            var xlRow = ws.Row(r);
            if (xlRow.IsEmpty()) continue;
            result.Add(MapCustomer(r, col =>
                headers.TryGetValue(Normalise(col), out var colNum)
                    ? xlRow.Cell(colNum).GetFormattedString().Trim()
                    : string.Empty));
        }

        return result;
    }

    private static CustomerProfile MapCustomer(int rowNumber, Func<string, string> get)
    {
        var depositText = get("Initial Deposit");
        decimal.TryParse(depositText, NumberStyles.Any, CultureInfo.InvariantCulture, out var deposit);

        var dobRaw = get("DOB");

        return new CustomerProfile
        {
            RowNumber = rowNumber,
            FirstName = get("First Name"),
            LastName = get("Last Name"),
            Address = get("Address"),
            City = get("City"),
            State = get("State"),
            ZipCode = get("Zip Code"),
            PhoneNumber = get("Phone Number"),
            Ssn = get("SSN"),
            Username = get("Username"),
            Password = get("Password"),
            AccountType = get("Account Type"),
            InitialDepositUsd = deposit,
            DobRaw = dobRaw,
            DateOfBirth = ParseDob(dobRaw),
            DebitCardNumber = get("Debit Card"),
            Cvv = get("CVV")
        };
    }

    private static DateTime? ParseDob(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var formats = new[]
        {
            "MM-dd-yy", "M-d-yy", "dd/MM/yyyy", "d/M/yyyy",
            "MMMM d, yyyy", "MMM d, yyyy", "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(value.Trim(), formats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dt2) ? dt2 : null;
    }

    private static string Normalise(string s) =>
        s.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
