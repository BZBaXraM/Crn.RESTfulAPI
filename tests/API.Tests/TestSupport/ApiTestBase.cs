using API.Tests.TestSupport;
using Xunit;

namespace API.Tests.TestSupport;

/// <summary>Base class giving every test class its own factory + isolated Sqlite database and HttpClient.</summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory = new();
    protected HttpClient Client = null!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeDatabaseAsync();
        Client = Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
