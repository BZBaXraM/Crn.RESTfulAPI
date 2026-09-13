using System.Net.Http.Json;
using Application.DTOs;

namespace API.Tests.TestSupport;

public static class AuthHttpClientExtensions
{
    public static async Task<AuthResponse> RegisterAndLoginAsync(
        this HttpClient client, string userName = "alice", string password = "Passw0rd!")
    {
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(userName, $"{userName}@example.com", password));
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(userName, password));
        login.EnsureSuccessStatusCode();

        return (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static void UseBearerToken(this HttpClient client, string accessToken)
        => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
}
