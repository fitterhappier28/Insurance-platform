using Platform.Tenancy;
using Platform.Web;
using Policy.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddTenancy();
builder.AddClpWeb(typeof(Program).Assembly, "Policy.Api.policy.v1.yaml");
await builder.AddPolicyDatabaseAsync();

var app = builder.Build();
app.UseClpPipeline();
app.Run();

public partial class Program;
