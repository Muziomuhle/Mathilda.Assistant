namespace Mathilda.Models
{
    public class ExecutionSummary
    {
        public List<TimeEntryResponse> TimeEntryResponses { get; set; } = new();
        public List<TimeEntryEvent> TimeEntryFailures { get; set; } = new();
        public int TotalSuccess { get; set; }
        public int TotalFailed { get; set; }
        public int TotalRequested { get; set; }
    }
}
