using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class StatisticsSummary
    {
        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int Info { get; set; }

        [JsonPropertyName("error_percentage")] public double ErrorPercentage { get; set; }

        [JsonPropertyName("warning_percentage")]
        public double WarningPercentage { get; set; }
    }
}
