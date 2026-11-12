using System.Text.Json.Serialization;
using Platform.Tenancy;
using Platform.Web;
using Rating.Api;
using Rating.Application;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddTenancy();
builder.AddClpWeb(typeof(Program).Assembly, "Rating.Api.rating.v1.yaml");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddSingleton<RatingService>();

var app = builder.Build();
app.UseClpPipeline();
app.MapRatingApi();
app.Run();

public partial class Program;
