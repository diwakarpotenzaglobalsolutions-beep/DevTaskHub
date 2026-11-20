using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DevTaskHubBackend.Data;

// MySql.Data returns DATE values as DateTime, so DateOnly cannot be read directly.
public sealed class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
{
    public DateOnlyConverter()
        : base(
            date => date.ToDateTime(TimeOnly.MinValue),
            value => DateOnly.FromDateTime(value))
    {
    }
}
