using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Web;

namespace Ledger.Infrastructure;

public static class LedgerDatabase
{
    public const string ConnectionName = "ledger-db";

    public static async Task AddLedgerDatabaseAsync(this WebApplicationBuilder builder)
    {
        var ownerConnection = builder.Configuration["ConnectionStrings:ledger-owner"]
            ?? BuildLoginConnection(builder, "Owner");
        var runtimeConnection = builder.Configuration["ConnectionStrings:ledger-runtime"]
            ?? BuildLoginConnection(builder, "Runtime");
        if (string.IsNullOrWhiteSpace(ownerConnection) || string.IsNullOrWhiteSpace(runtimeConnection))
        {
            return;
        }

        var owner = new DbContextOptionsBuilder<LedgerDbContext>().UseSqlServer(ownerConnection).Options;
        await using (var context = new LedgerDbContext(owner))
        {
            await context.Database.MigrateAsync();
        }

        builder.Services.AddDbContext<LedgerDbContext>(options => options.UseSqlServer(runtimeConnection));
        builder.Services.AddHealthChecks().AddCheck("database", new SqlConnectionHealthCheck(runtimeConnection));
        var kafka = builder.Configuration["ConnectionStrings:kafka"];
        if (!string.IsNullOrWhiteSpace(kafka))
        {
            builder.Services.AddHealthChecks().AddCheck("kafka", new KafkaHealthCheck(KafkaEndpoint.Parse(kafka)));
        }
    }

    private static string? BuildLoginConnection(WebApplicationBuilder builder, string loginKind)
    {
        var baseConnection = builder.Configuration[$"ConnectionStrings:{ConnectionName}"];
        var user = builder.Configuration[$"LedgerDatabase:{loginKind}User"];
        var password = builder.Configuration[$"LedgerDatabase:{loginKind}Password"];
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
