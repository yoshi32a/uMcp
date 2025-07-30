using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class TestRunResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        [JsonPropertyName("test_mode")] public string TestMode { get; set; }

        [JsonPropertyName("timeout_seconds")] public int TimeoutSeconds { get; set; }

        public TestSummary Summary { get; set; }

        [JsonPropertyName("failed_tests")] public List<FailedTestDetail> FailedTests { get; set; }

        [JsonPropertyName("start_time")] public string StartTime { get; set; }

        [JsonPropertyName("end_time")] public string EndTime { get; set; }

        [JsonPropertyName("overall_result")] public string OverallResult { get; set; }
    }
}