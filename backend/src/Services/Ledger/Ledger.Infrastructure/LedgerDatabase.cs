using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Web;

namespace Ledger.Infrastructure;

public static class LedgerDatabase
{
    public const string ConnectionName = "ledger-db";

    public static async Task AddLedgerDatabaseAsync(this WebApplicationBuilder builder)
    {
        var ownerConnection = builder.Configuration["ConnectionStrings:ledger-owner"];
        var runtimeConnection = builder.Configuration["ConnectionStrings:ledger-runtime"];
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
}
