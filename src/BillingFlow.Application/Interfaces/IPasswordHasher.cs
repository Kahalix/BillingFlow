// File: src/BillingFlow.Application/Interfaces/IPasswordHasher.cs
namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Abstraction for password hashing algorithms.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
