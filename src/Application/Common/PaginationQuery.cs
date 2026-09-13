namespace Application.Common;

public sealed record PaginationQuery
{
    public const int MaxPageSize = 100;

    private readonly int _pageSize = 10;

    public int PageNumber { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }
}
