namespace Policy.Infrastructure;

public sealed class PolicyAccountEntity
{
    public Guid AccountId { get; set; }
    public Guid ApplicantId { get; set; }
    public required string TenantId { get; set; }
    public required string Name { get; set; }
    public required string ApplicantLegalName { get; set; }
    public required string ApplicantFein { get; set; }
    public required string MailingAddressJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PolicyRevisionEntity
{
    public Guid QuoteId { get; set; }
    public Guid PolicyId { get; set; }
    public required string TenantId { get; set; }
    public Guid AccountId { get; set; }
    public int RevisionNo { get; set; }
    public required string Status { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
    public DateTimeOffset QuotedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public required string DecisionJson { get; set; }
    public string? RatingJson { get; set; }
    public required string ScheduleJson { get; set; }
    public string? PolicyNumber { get; set; }
    public DateTimeOffset? BoundAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class PolicyBookingStatusEntity
{
    public required string TenantId { get; set; }
    public Guid PolicyId { get; set; }
    public int RevisionNo { get; set; }
    public required string Status { get; set; }
    public Guid? TransactionId { get; set; }
    public DateTimeOffset? BookedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PolicyIdempotencyEntity
{
    public long Id { get; set; }
    public required string TenantId { get; set; }
    public required string Operation { get; set; }
    public required string Key { get; set; }
    public required string RequestHash { get; set; }
    public int ResponseStatus { get; set; }
    public required string ResponseJson { get; set; }
    public required string ResourcePath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PolicyOutboxEntity
{
    public Guid MessageId { get; set; }
    public required string TenantId { get; set; }
    public required string Type { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
