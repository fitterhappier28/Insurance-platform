using System.Text.RegularExpressions;

namespace Policy.Domain;

public sealed record Applicant(Guid ApplicantId, string LegalName, string Fein, Address MailingAddress);

public sealed record Account(
    Guid AccountId,
    string TenantId,
    string Name,
    Applicant Applicant,
    DateTimeOffset CreatedAt)
{
    public static Account Create(string name, ApplicantInput applicant, string tenantId, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId)
            || !Regex.IsMatch(tenantId, "^[a-z][a-z0-9-]{1,31}$", RegexOptions.CultureInvariant))
        {
            throw new ArgumentException("Tenant identifier is invalid.", nameof(tenantId));
        }
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200
            || string.IsNullOrWhiteSpace(applicant.LegalName) || applicant.LegalName.Length > 200
            || !Regex.IsMatch(applicant.Fein, "^\\d{2}-\\d{7}$", RegexOptions.CultureInvariant))
        {
            throw new ArgumentException("Account or Applicant fields are invalid.");
        }

        applicant.MailingAddress.Validate();
        var utcCreatedAt = createdAt.ToUniversalTime();
        return new Account(Guid.NewGuid(), tenantId, name,
            new Applicant(Guid.NewGuid(), applicant.LegalName, applicant.Fein, applicant.MailingAddress), utcCreatedAt);
    }
}

public sealed record ApplicantInput(string LegalName, string Fein, Address MailingAddress);
