using System.Text.RegularExpressions;

namespace Policy.Domain;

public sealed record Address(string Street, string City, string State, string PostalCode)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Street) || Street.Length > 200
            || string.IsNullOrWhiteSpace(City) || City.Length > 100
            || !Regex.IsMatch(State, "^[A-Z]{2}$", RegexOptions.CultureInvariant)
            || !Regex.IsMatch(PostalCode, "^\\d{5}(-\\d{4})?$", RegexOptions.CultureInvariant))
        {
            throw new ArgumentException("Address fields are invalid.");
        }
    }
}
