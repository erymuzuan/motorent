namespace MotoRent.Domain.Extensions;

public static class DateTimeExtensions
{
    extension(DateTime date)
    {
        /// <summary>
        /// Converts a Buddhist Era year (พ.ศ.) to Common Era (ค.ศ.) if the year exceeds 2500.
        /// Thai locale browsers may return dates with Buddhist years (e.g. 2569 instead of 2026).
        /// </summary>
        public DateTime NormalizeBuddhistYear()
            => date.Year > 2500 ? date.AddYears(-543) : date;
    }
}
