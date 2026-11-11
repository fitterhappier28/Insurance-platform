namespace Platform.Tenancy;

public interface ITenantContext
{
    string? TenantId { get; }
}

public sealed class TenantContext : ITenantContext
{
    public string? TenantId { get; private set; }

    public void Set(string tenantId) => TenantId = tenantId;
}
