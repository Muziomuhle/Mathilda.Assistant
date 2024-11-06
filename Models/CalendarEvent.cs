namespace Mathilda.Models
{
    public class CalendarEvent
    {
        public string Summary { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class CalendarFilters
    {
        public bool EnableFilters { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
