using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class LogStatisticsResponse
    {
        public bool Success { get; set; }

        [JsonPropertyName("total_logs")] public int TotalLogs { get; set; }

        [JsonPropertyName("sample_size")] public int SampleSize { get; set; }

        public StatisticsSummary Statistics { get; set; }
        public string Timestamp { get; set; }
    }
}
