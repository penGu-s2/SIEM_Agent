using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIEM_Agent.Core.Entities;

namespace SIEM_Agent.Core.Interfaces
{
    public interface ILogCollector
    {
        string CollectorType { get; }
        event EventHandler<LogEntry> OnLogReceived;
        Task StartAsync();
        Task StopAsync();
        Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime startTime, DateTime endTime);
        Task<LogEntry> GetLogByIdAsync(string id);
    }
} 