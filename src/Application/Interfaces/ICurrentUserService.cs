namespace Application.Interfaces;

public interface ICurrentUserService
{
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
