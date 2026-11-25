namespace CleanDistrict.Web.Models;

public class PagedList<T> : List<T>
{
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}
