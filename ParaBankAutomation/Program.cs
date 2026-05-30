using ParaBankAutomation.Abstractions;
using ParaBankAutomation.Hubs;
using ParaBankAutomation.Rpa;
using ParaBankAutomation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ICustomerSourceReader, CsvReaderService>();
builder.Services.AddSingleton<IExchangeRateProvider, CurrencyService>();
builder.Services.AddSingleton<IOperationReportWriter, ExcelReportWriter>();
builder.Services.AddSingleton<ICustomerValidationService, CustomerValidationService>();
builder.Services.AddSingleton<ICustomerAutomationServiceFactory, ParaBankAutomationServiceFactory>();
builder.Services.AddSingleton<AutomationOrchestrator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapHub<ProgressHub>("/progressHub");

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
