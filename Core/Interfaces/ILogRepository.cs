using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIEM_Agent.Core.Entities;

namespace SIEM_Agent.Core.Interfaces
{
    public interface ILogRepository
    {
        Task SaveLogAsync(LogEntry log);
        Task<IEnumerable<LogEntry>> GetLogsByTypeAsync(string logType, DateTime startTime, DateTime endTime);
        Task<LogEntry> GetLogByIdAsync(string id);
        Task DeleteLogsByTypeAsync(string logType, DateTime startTime, DateTime endTime);
    }
} 