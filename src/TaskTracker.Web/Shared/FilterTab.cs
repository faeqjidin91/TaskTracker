namespace TaskTracker.Web.Shared;

public sealed record FilterTab(string Key, string Label, int? Count = null);

public static class Paging
{
    public const int PageSize = 10;

    public static int TotalPages(int total) => Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

    // Keeps the page valid when filtering or deleting shrinks the list.
    public static int Clamp(int page, int total) => Math.Clamp(page, 1, TotalPages(total));

    public static List<T> Slice<T>(IReadOnlyCollection<T> items, int page) =>
        items.Skip((Clamp(page, items.Count) - 1) * PageSize).Take(PageSize).ToList();
}
