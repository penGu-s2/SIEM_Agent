using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIEM_Agent.Core.Repositories
{
    public interface ILogRepository
    {
        Task SaveLogAsync(string logType, string message);
        Task<IEnumerable<string>> GetLogsAsync(string logType, DateTime startTime, DateTime endTime);
        Task ClearLogsAsync(string logType);
    }
} 