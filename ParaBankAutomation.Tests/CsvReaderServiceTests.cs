using Microsoft.Extensions.Logging.Abstractions;
using ParaBankAutomation.Services;
using Xunit;

namespace ParaBankAutomation.Tests;

public sealed class CsvReaderServiceTests
{
    [Fact]
    public void ReadCustomers_MapsHeadersDespiteBlankColumns()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"First{unique}";
        var lastName = $"Last{unique}";
        var username = $"user_{unique}";
        var initialDeposit = 500m;
        var dob = new DateTime(1980, 1, 1);

        var csv = string.Join(Environment.NewLine,
            "First Name,Last Name,Address,City,State,Zip Code,Phone Number,SSN,Username,Password,Account Type,Initial Deposit,,,,DOB,Debit Card,CVV",
            $"{firstName},{lastName},Test Address,Test City,TS,00000,0000000000,000-00-0000,{username},SyntheticPassword!,Checking,{initialDeposit},,,,01-01-80,0000 0000 0000 0000,000");

        var path = Path.Combine(Path.GetTempPath(), $"parabank_test_customers_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, csv);

        try
        {
            var reader = new CsvReaderService(NullLogger<CsvReaderService>.Instance);

            var customers = reader.ReadCustomers(path);

            var customer = Assert.Single(customers);
            Assert.Equal($"{firstName} {lastName}", customer.FullName);
            Assert.Equal(username, customer.Username);
            Assert.Equal(initialDeposit, customer.InitialDepositUsd);
            Assert.Equal(dob, customer.DateOfBirth);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
