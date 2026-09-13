using Infrastructure.Identity;
using Xunit;

namespace Infrastructure.Tests.Identity;

public class PasswordHasherAdapterTests
{
    private readonly PasswordHasherAdapter _sut = new();

    [Fact]
    public void Verify_succeeds_for_the_password_that_was_hashed()
    {
        var hash = _sut.Hash("Correct-Password1");

        Assert.True(_sut.Verify(hash, "Correct-Password1"));
    }

    [Fact]
    public void Verify_fails_for_a_different_password()
    {
        var hash = _sut.Hash("Correct-Password1");

        Assert.False(_sut.Verify(hash, "Wrong-Password1"));
    }

    [Fact]
    public void Hash_does_not_store_the_password_in_plain_text()
    {
        var hash = _sut.Hash("Correct-Password1");

        Assert.DoesNotContain("Correct-Password1", hash);
    }

    [Fact]
    public void Hash_is_salted_so_the_same_password_hashes_differently_each_time()
    {
        var first = _sut.Hash("Correct-Password1");
        var second = _sut.Hash("Correct-Password1");

        Assert.NotEqual(first, second);
    }
}
