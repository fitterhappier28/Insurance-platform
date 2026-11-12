using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Policy.Infrastructure;

[DbContext(typeof(PolicyDbContext))]
[Migration("20261112000000_PolicyWorkflow")]
public sealed class PolicyWorkflowMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PolicyAccounts",
            columns: table => new
            {
                AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ApplicantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ApplicantLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ApplicantFein = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                MailingAddressJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PolicyAccounts", x => x.AccountId));

        migrationBuilder.CreateTable(
            name: "PolicyBookingStatuses",
            columns: table => new
            {
                TenantId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RevisionNo = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                BookedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PolicyBookingStatuses", x => new { x.TenantId, x.PolicyId, x.RevisionNo }));

        migrationBuilder.CreateTable(
            name: "PolicyIdempotency",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                TenantId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Operation = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ResponseStatus = table.Column<int>(type: "int", nullable: false),
                ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ResourcePath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PolicyIdempotency", x => x.Id));

        migrationBuilder.CreateTable(
            name: "PolicyOutbox",
            columns: table => new
            {
                MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_PolicyOutbox", x => x.MessageId));

        migrationBuilder.CreateTable(
            name: "PolicyRevisions",
            columns: table => new
            {
                QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RevisionNo = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                EffectiveTo = table.Column<DateOnly>(type: "date", nullable: false),
                QuotedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DecisionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                RatingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PolicyNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                BoundAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PolicyRevisions", x => x.QuoteId);
                table.ForeignKey("FK_PolicyRevisions_PolicyAccounts_AccountId", x => x.AccountId,
                    "PolicyAccounts", "AccountId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_PolicyAccounts_TenantId_AccountId", "PolicyAccounts",
            new[] { "TenantId", "AccountId" }, unique: true);
        migrationBuilder.CreateIndex("IX_PolicyBookingStatuses_TenantId_Status_CreatedAt", "PolicyBookingStatuses",
            new[] { "TenantId", "Status", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_PolicyIdempotency_TenantId_Operation_Key", "PolicyIdempotency",
            new[] { "TenantId", "Operation", "Key" }, unique: true);
        migrationBuilder.CreateIndex("IX_PolicyOutbox_PublishedAt_CreatedAt", "PolicyOutbox", new[] { "PublishedAt", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_PolicyRevisions_AccountId", "PolicyRevisions", "AccountId");
        migrationBuilder.CreateIndex("IX_PolicyRevisions_TenantId_PolicyId_RevisionNo", "PolicyRevisions",
            new[] { "TenantId", "PolicyId", "RevisionNo" }, unique: true);
        migrationBuilder.CreateIndex("IX_PolicyRevisions_PolicyNumber", "PolicyRevisions", "PolicyNumber",
            unique: true, filter: "[PolicyNumber] IS NOT NULL");
        migrationBuilder.Sql("""
            CREATE TRIGGER dbo.TR_PolicyRevisions_BoundImmutable
            ON dbo.PolicyRevisions
            AFTER UPDATE, DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS
                (
                    SELECT 1
                    FROM deleted d
                    LEFT JOIN inserted i ON i.QuoteId = d.QuoteId
                    WHERE i.QuoteId IS NULL
                       OR d.Status = N'Bound'
                       OR i.TenantId <> d.TenantId
                       OR i.PolicyId <> d.PolicyId
                       OR i.AccountId <> d.AccountId
                       OR i.RevisionNo <> d.RevisionNo
                       OR i.EffectiveFrom <> d.EffectiveFrom
                       OR i.EffectiveTo <> d.EffectiveTo
                       OR i.QuotedAt <> d.QuotedAt
                       OR i.ExpiresAt <> d.ExpiresAt
                       OR ISNULL(i.DecisionJson, N'') <> ISNULL(d.DecisionJson, N'')
                       OR ISNULL(i.RatingJson, N'') <> ISNULL(d.RatingJson, N'')
                       OR i.ScheduleJson <> d.ScheduleJson
                       OR i.CreatedAt <> d.CreatedAt
                       OR (i.Status <> d.Status AND NOT (d.Status = N'Quoted' AND i.Status = N'Bound'))
                )
                BEGIN
                    THROW 51013, 'Policy Revision terms are immutable after quote creation and after bind.', 1;
                END
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_PolicyRevisions_BoundImmutable;");
        migrationBuilder.DropTable("PolicyIdempotency");
        migrationBuilder.DropTable("PolicyBookingStatuses");
        migrationBuilder.DropTable("PolicyOutbox");
        migrationBuilder.DropTable("PolicyRevisions");
        migrationBuilder.DropTable("PolicyAccounts");
    }
}
