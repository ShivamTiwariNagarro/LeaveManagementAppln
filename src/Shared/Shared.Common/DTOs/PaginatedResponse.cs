namespace Shared.Common.DTOs;

/// <summary>
/// Paginated response wrapper for list endpoints
/// </summary>
/// <typeparam name="T">Type of items in the list</typeparam>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PaginatedResponse<T> Create(List<T> items, int page, int pageSize, int totalCount)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public static PaginatedResponse<T> Empty(int page = 1, int pageSize = 10)
    {
        return new PaginatedResponse<T>
        {
            Items = new List<T>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0
        };
    }
}

/// <summary>
/// Pagination request parameters
/// </summary>
public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 10;
    private const int MaxPageSize = 50;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }

    public int Skip => (Page - 1) * PageSize;
}
