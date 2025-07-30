using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class TestSummary
    {
        [JsonPropertyName("total_tests")] public int TotalTests { get; set; }

        [JsonPropertyName("passed_tests")] public int PassedTests { get; set; }

        [JsonPropertyName("failed_tests")] public int FailedTests { get; set; }

        [JsonPropertyName("skipped_tests")] public int SkippedTests { get; set; }

        [JsonPropertyName("duration_seconds")] public double DurationSeconds { get; set; }

        [JsonPropertyName("success_rate")] public double SuccessRate { get; set; }
    }
}