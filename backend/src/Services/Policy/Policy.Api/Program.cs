using Platform.Tenancy;
using Platform.Web;
using Policy.Infrastructure;
using Policy.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddTenancy();
builder.AddClpWeb(typeof(Program).Assembly, "Policy.Api.policy.v1.yaml");
builder.Services.AddHttpClient("rules", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PolicyDependencies:RulesBaseUrl"] ?? "http://localhost:5102/");
    client.Timeout = TimeSpan.FromSeconds(2);
});
builder.Services.AddHttpClient("rating", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PolicyDependencies:RatingBaseUrl"] ?? "http://localhost:5103/");
    client.Timeout = TimeSpan.FromSeconds(2);
});
await builder.AddPolicyDatabaseAsync();

var app = builder.Build();
app.UseClpPipeline();
app.MapPolicyApi();
app.Run();

public partial class Program;
