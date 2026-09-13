using Application.DTOs;
using Application.Validators;
using Xunit;

namespace Application.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    [Fact]
    public void Passes_for_valid_request()
        => Assert.True(_sut.Validate(new RegisterRequest("alice", "alice@example.com", "Passw0rd!")).IsValid);

    [Theory]
    [InlineData("password")]  // no uppercase, no digit
    [InlineData("PASSWORD1")] // no lowercase
    [InlineData("Passw0r")]   // too short
    public void Fails_for_weak_passwords(string password)
        => Assert.False(_sut.Validate(new RegisterRequest("alice", "alice@example.com", password)).IsValid);

    [Fact]
    public void Fails_for_invalid_email()
        => Assert.False(_sut.Validate(new RegisterRequest("alice", "not-an-email", "Passw0rd!")).IsValid);

    [Fact]
    public void Fails_for_username_with_illegal_characters()
        => Assert.False(_sut.Validate(new RegisterRequest("alice smith!", "alice@example.com", "Passw0rd!")).IsValid);
}
