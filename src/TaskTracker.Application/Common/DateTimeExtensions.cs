namespace TaskTracker.Application.Common;

public static class DateTimeExtensions
{
    // SQL Server returns DateTimeKind.Unspecified. Without this, API dates would serialize
    // with no "Z" and clients would read them as local time.
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? AsUtc(this DateTime? value) => value?.AsUtc();
}
