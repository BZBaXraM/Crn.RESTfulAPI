using Application.Interfaces;

namespace Application.Tests.TestSupport;

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public string? UserName { get; set; } = "tester";
    public bool IsAuthenticated { get; set; } = true;
}
