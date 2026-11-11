using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Policy.Infrastructure;

[DbContext(typeof(PolicyDbContext))]
[Migration("20261111000000_Initial")]
public sealed class InitialMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
