using Policy.Domain;
using Shouldly;

namespace Policy.Tests;

public sealed class FR02_AccountTests
{
    [Fact]
    public void FR02_account_and_applicant_are_created_with_tenant_and_utc_time()
    {
        var createdAt = new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.Zero);

        var account = Account.Create(
            "Bayview Properties LLC",
            new ApplicantInput("Bayview Properties LLC", "12-3456789",
                new Address("100 Bay St", "Tampa", "FL", "33602")),
            "harbor",
            createdAt);

        account.TenantId.ShouldBe("harbor");
        account.Name.ShouldBe("Bayview Properties LLC");
        account.Applicant.Fein.ShouldBe("12-3456789");
        account.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
    }
}
