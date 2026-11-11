using Platform.Tenancy;
using Platform.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddTenancy();
builder.AddClpWeb(typeof(Program).Assembly, "Rules.Api.rules.v1.yaml");

var app = builder.Build();
app.UseClpPipeline();
app.Run();

public partial class Program;
