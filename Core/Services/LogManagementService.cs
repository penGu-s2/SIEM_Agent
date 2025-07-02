using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIEM_Agent.Core.Repositories;

namespace SIEM_Agent.Core.Services
{
    public class LogManagementService
    {
        private readonly ILogRepository _logRepository;

        public LogManagementService(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task SaveLogAsync(string logType, string message)
        {
            await _logRepository.SaveLogAsync(logType, message);
        }

        public async Task ClearLogsAsync(string logType)
        {
            await _logRepository.ClearLogsAsync(logType);
        }

        public async Task<IEnumerable<string>> GetLogsAsync(string logType, DateTime startTime, DateTime endTime)
        {
            return await _logRepository.GetLogsAsync(logType, startTime, endTime);
        }
    }
} 