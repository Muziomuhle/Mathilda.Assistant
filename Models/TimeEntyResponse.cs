using System.Text.Json.Serialization;

namespace Mathilda.Models
{
    public class TimeInterval
    {
        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime End { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }
    }

    public class TimeEntryResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("tagIds")]
        public List<string> TagIds { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("billable")]
        public bool Billable { get; set; }

        [JsonPropertyName("taskId")]
        public string TaskId { get; set; }

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }

        [JsonPropertyName("timeInterval")]
        public TimeInterval TimeInterval { get; set; }

        [JsonPropertyName("workspaceId")]
        public string WorkspaceId { get; set; }

        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; }

        [JsonPropertyName("customFieldValues")]
        public List<object> CustomFieldValues { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("kioskId")]
        public string KioskId { get; set; }
    }
}
