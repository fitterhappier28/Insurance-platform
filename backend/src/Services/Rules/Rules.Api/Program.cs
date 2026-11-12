using System.Text.Json.Serialization;
using Platform.Tenancy;
using Platform.Web;
using Rules.Api;
using Rules.Application;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddTenancy();
builder.AddClpWeb(typeof(Program).Assembly, "Rules.Api.rules.v1.yaml");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddSingleton<DecisionService>();

var app = builder.Build();
app.UseClpPipeline();
app.MapRulesApi();
app.Run();

public partial class Program;
