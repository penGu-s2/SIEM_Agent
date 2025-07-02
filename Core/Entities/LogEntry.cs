using System;

namespace SIEM_Agent.Core.Entities
{
    public class LogEntry
    {
        public string Id { get; set; } = string.Empty;
        public string LogType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RawData { get; set; } = string.Empty;
    }
} 