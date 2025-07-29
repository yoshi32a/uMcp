using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class FailedTestDetail
    {
        public string Name { get; set; }
        
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }
        
        public string Message { get; set; }
        
        [JsonPropertyName("stack_trace")]
        public string StackTrace { get; set; }
        
        public double Duration { get; set; }
    }
}