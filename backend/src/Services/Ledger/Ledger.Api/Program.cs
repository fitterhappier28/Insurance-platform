using Ledger.Infrastructure;
using Platform.Tenancy;
using Platform.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddTenancy();
builder.AddClpWeb(typeof(Program).Assembly, "Ledger.Api.ledger.v1.yaml");
await builder.AddLedgerDatabaseAsync();

var app = builder.Build();
app.UseClpPipeline();
app.Run();

public partial class Program;
