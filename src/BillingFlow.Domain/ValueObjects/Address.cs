// File: src/BillingFlow.Domain/ValueObjects/Address.cs
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.ValueObjects;

/// <summary>
/// Value Object representing a physical billing address.
/// Uses C# record semantics for structural equality.
/// </summary>
public record Address
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;

    // Required by EF Core
    private Address() { }

    public Address(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("Street cannot be empty or whitespace.");

        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City cannot be empty or whitespace.");

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new DomainException("Postal code cannot be empty or whitespace.");

        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Country cannot be empty or whitespace.");

        Street = street.Trim();
        City = city.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
    }
}
