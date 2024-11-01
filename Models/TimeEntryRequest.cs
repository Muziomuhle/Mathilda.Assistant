using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace Mathilda.Models
{
    public class TimeEntryRequest
    {
        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime Start { get; set; }
        [Required]
        public DateTime End { get; set; }
        [Required]
        public string TaskName { get; set; }
    }

    public class ReccurringTimeEntryRequest : TimeEntryRequest
    {
        public List<DayOfWeek> daysOfWeek { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        /// <summary>
        /// Interval for meeting. 1 = Day, 14 = BiWeekly
        /// </summary>
        [Required]
        public int Interval { get; set; }
    }
}
