using System.Globalization;

namespace Mathilda.Extensions
{
    public static class DateTimeExtensions
    {
        public static string FormatDateToIso8601(DateTime date)
        {
            return date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        public static DateTime ParseIso8601ToDate(string dateString)
        {
            return DateTime.ParseExact(dateString, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }

        public static DateTime ParseDateTime(string dateTimeString, DateTimeStyles dateTimeStyles = DateTimeStyles.AssumeUniversal)
        {
            // Try parsing with different formats
            string[] formats = { "yyyyMMddTHHmmssZ", "yyyyMMddTHHmmss", "yyyyMMdd", "yyyyMMddTHHmm", "yyyy-MM-ddTHH:mm" };
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture, dateTimeStyles, out DateTime dateTime))
                {
                    return dateTime;
                }
            }

            throw new FormatException($"String '{dateTimeString}' was not recognized as a valid DateTime.");
        }
    }
}
