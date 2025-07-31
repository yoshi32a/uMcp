using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class ConsoleLogResponse
    {
        public bool Success { get; set; }

        [JsonPropertyName("total_logs_in_console")]
        public int TotalLogsInConsole { get; set; }

        [JsonPropertyName("retrieved_logs")] public int RetrievedLogs { get; set; }

        public LogSummary Summary { get; set; }
        public List<LogEntry> Logs { get; set; }
    }
}
