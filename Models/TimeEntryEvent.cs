using System.Text.Json.Serialization;

namespace Mathilda.Models
{
    public class TimeEntryEvent
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime End { get; set; }

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }

        [JsonPropertyName("taskId")]
        public string TaskId { get; set; }
    }
}
