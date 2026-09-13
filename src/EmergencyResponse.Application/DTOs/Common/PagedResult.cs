namespace EmergencyResponse.Application.DTOs.Common;

public sealed record PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; }
        = Array.Empty<T>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount / (double)PageSize);
}