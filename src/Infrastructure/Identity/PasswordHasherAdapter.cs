using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>Wraps ASP.NET Core Identity's PBKDF2 password hasher behind the application-layer abstraction.</summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();
    private static readonly User Dummy = new();

    public string Hash(string password) => _inner.HashPassword(Dummy, password);

    public bool Verify(string hash, string password)
        => _inner.VerifyHashedPassword(Dummy, hash, password) is not PasswordVerificationResult.Failed;
}
