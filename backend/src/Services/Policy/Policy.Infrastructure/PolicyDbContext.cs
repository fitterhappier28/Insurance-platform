using Microsoft.EntityFrameworkCore;

namespace Policy.Infrastructure;

public sealed class PolicyDbContext(DbContextOptions<PolicyDbContext> options) : DbContext(options)
{
    public DbSet<PolicyAccountEntity> Accounts => Set<PolicyAccountEntity>();
    public DbSet<PolicyRevisionEntity> Revisions => Set<PolicyRevisionEntity>();
    public DbSet<PolicyBookingStatusEntity> BookingStatuses => Set<PolicyBookingStatusEntity>();
    public DbSet<PolicyIdempotencyEntity> Idempotency => Set<PolicyIdempotencyEntity>();
    public DbSet<PolicyOutboxEntity> Outbox => Set<PolicyOutboxEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PolicyAccountEntity>(entity =>
        {
            entity.ToTable("PolicyAccounts");
            entity.HasKey(item => item.AccountId);
            entity.Property(item => item.TenantId).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ApplicantLegalName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ApplicantFein).HasMaxLength(10).IsRequired();
            entity.Property(item => item.MailingAddressJson).IsRequired();
            entity.HasIndex(item => new { item.TenantId, item.AccountId }).IsUnique();
        });

        modelBuilder.Entity<PolicyRevisionEntity>(entity =>
        {
            entity.ToTable("PolicyRevisions");
            entity.HasKey(item => item.QuoteId);
            entity.Property(item => item.TenantId).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(24).IsRequired();
            entity.Property(item => item.PolicyNumber).HasMaxLength(16);
            entity.Property(item => item.DecisionJson).IsRequired();
            entity.Property(item => item.ScheduleJson).IsRequired();
            entity.Property(item => item.RatingJson);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.TenantId, item.PolicyId, item.RevisionNo }).IsUnique();
            entity.HasIndex(item => item.PolicyNumber).IsUnique().HasFilter("[PolicyNumber] IS NOT NULL");
            entity.HasIndex(item => new { item.TenantId, item.AccountId });
            entity.HasOne<PolicyAccountEntity>().WithMany().HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyBookingStatusEntity>(entity =>
        {
            entity.ToTable("PolicyBookingStatuses");
            entity.HasKey(item => new { item.TenantId, item.PolicyId, item.RevisionNo });
            entity.Property(item => item.TenantId).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.TenantId, item.Status, item.CreatedAt });
        });

        modelBuilder.Entity<PolicyIdempotencyEntity>(entity =>
        {
            entity.ToTable("PolicyIdempotency");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TenantId).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Key).HasMaxLength(64).IsRequired();
            entity.Property(item => item.RequestHash).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ResponseJson).IsRequired();
            entity.Property(item => item.ResourcePath).HasMaxLength(256).IsRequired();
            entity.HasIndex(item => new { item.TenantId, item.Operation, item.Key }).IsUnique();
        });

        modelBuilder.Entity<PolicyOutboxEntity>(entity =>
        {
            entity.ToTable("PolicyOutbox");
            entity.HasKey(item => item.MessageId);
            entity.Property(item => item.TenantId).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Type).HasMaxLength(128).IsRequired();
            entity.Property(item => item.PayloadJson).IsRequired();
            entity.HasIndex(item => new { item.PublishedAt, item.CreatedAt });
        });
    }
}
