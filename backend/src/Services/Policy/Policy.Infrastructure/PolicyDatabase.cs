using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Web;

namespace Policy.Infrastructure;

public static class PolicyDatabase
{
    public const string ConnectionName = "policy-db";

    public static async Task AddPolicyDatabaseAsync(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<PolicyDbContext>();
        var ownerConnection = builder.Configuration["ConnectionStrings:policy-owner"]
            ?? BuildLoginConnection(builder, "Owner");
        var runtimeConnection = builder.Configuration["ConnectionStrings:policy-runtime"]
            ?? BuildLoginConnection(builder, "Runtime");
        if (string.IsNullOrWhiteSpace(ownerConnection) || string.IsNullOrWhiteSpace(runtimeConnection))
        {
            return;
        }

        var owner = new DbContextOptionsBuilder<PolicyDbContext>().UseSqlServer(ownerConnection).Options;
        await using (var context = new PolicyDbContext(owner))
        {
            await context.Database.MigrateAsync();
        }

        builder.Services.AddDbContext<PolicyDbContext>(options => options.UseSqlServer(runtimeConnection));
        builder.Services.AddHealthChecks().AddCheck("database", new SqlConnectionHealthCheck(runtimeConnection));
        AddKafkaReadyCheck(builder);
    }

    public static void AddKafkaReadyCheck(WebApplicationBuilder builder)
    {
        var kafka = builder.Configuration["ConnectionStrings:kafka"];
        if (!string.IsNullOrWhiteSpace(kafka))
        {
            builder.Services.AddHealthChecks().AddCheck("kafka", new KafkaHealthCheck(KafkaEndpoint.Parse(kafka)));
        }
    }

    private static string? BuildLoginConnection(WebApplicationBuilder builder, string loginKind)
    {
        var baseConnection = builder.Configuration[$"ConnectionStrings:{ConnectionName}"];
        var user = builder.Configuration[$"PolicyDatabase:{loginKind}User"];
        var password = builder.Configuration[$"PolicyDatabase:{loginKind}Password"];
        if (string.IsNullOrWhiteSpace(baseConnection) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            UserID = user,
            Password = password
        };
        return connection.ConnectionString;
    }
}
