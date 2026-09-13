using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using API.Tests.TestSupport;
using Xunit;

namespace API.Tests.Controllers;

public class AuthControllerTests : ApiTestBase
{
    [Fact]
    public async Task Register_returns_201_with_the_created_user()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("alice", "alice@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("alice", user!.UserName);
        Assert.Equal("User", user.Role);
    }

    [Fact]
    public async Task Register_returns_400_for_invalid_payload()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("a", "not-an-email", "weak"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_returns_409_for_duplicate_username()
    {
        await Client.RegisterAndLoginAsync("bob");

        var response = await Client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("bob", "someone-else@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_401_for_wrong_password()
    {
        await Client.RegisterAndLoginAsync("carol");

        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("carol", "WrongPass1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_a_usable_access_and_refresh_token()
    {
        var auth = await Client.RegisterAndLoginAsync("dave");

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.True(auth.AccessTokenExpiresOn > DateTime.UtcNow);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_the_old_one_can_no_longer_be_reused()
    {
        var auth = await Client.RegisterAndLoginAsync("erin");

        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotEqual(auth.RefreshToken, rotated!.RefreshToken);

        var reuseResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_reuse_of_a_rotated_token_also_revokes_its_successor()
    {
        var auth = await Client.RegisterAndLoginAsync("frank");
        var rotated = await (await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken)))
            .Content.ReadFromJsonAsync<AuthResponse>();

        // Reusing the original (now-revoked) token should be treated as theft and kill the new one too.
        await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));

        var secondRefreshAttempt = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(rotated!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshAttempt.StatusCode);
    }

    [Fact]
    public async Task Revoke_then_refresh_with_the_same_token_returns_401()
    {
        var auth = await Client.RegisterAndLoginAsync("grace");

        var revokeResponse = await Client.PostAsJsonAsync("/api/v1/auth/revoke", new RefreshTokenRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
