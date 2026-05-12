// File: src/BillingFlow.Infrastructure/Identity/PasswordHasher.cs
using BillingFlow.Application.Interfaces;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Implementation of password hashing using BCrypt.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // Work factor 12
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
