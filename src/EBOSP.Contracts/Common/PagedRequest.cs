namespace EBOSP.Contracts.Common;

/// <summary>
/// Standard pagination/sort query convention for every list endpoint (dev guide §9:
/// pagination/filter/sort conventions). Endpoint-specific filters extend this rather than
/// inventing their own paging parameters.
/// </summary>
public class PagedRequest
{
    private const int MaxPageSize = 200;
    private int _page = 1;
    private int _pageSize = 25;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 1,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    /// <summary>Field name to sort by; endpoint decides which field names are valid.</summary>
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}
