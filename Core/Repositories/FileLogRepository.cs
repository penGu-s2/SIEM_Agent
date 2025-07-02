using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SIEM_Agent.Core.Repositories
{
    public class FileLogRepository : ILogRepository
    {
        private readonly string _basePath;

        public FileLogRepository(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task SaveLogAsync(string logType, string message)
        {
            var filePath = Path.Combine(_basePath, $"{logType}.log");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"{timestamp}|{logType}|INFO|0|{message}{Environment.NewLine}";
            await File.AppendAllTextAsync(filePath, logEntry);
        }

        public async Task<IEnumerable<string>> GetLogsAsync(string logType, DateTime startTime, DateTime endTime)
        {
            var filePath = Path.Combine(_basePath, $"{logType}.log");
            if (!File.Exists(filePath))
            {
                return Enumerable.Empty<string>();
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            return lines.Where(line =>
            {
                if (string.IsNullOrWhiteSpace(line)) return false;
                var parts = line.Split('|');
                if (parts.Length < 1) return false;
                
                if (DateTime.TryParse(parts[0], out DateTime logTime))
                {
                    return logTime >= startTime && logTime <= endTime;
                }
                return false;
            });
        }

        public async Task ClearLogsAsync(string logType)
        {
            var filePath = Path.Combine(_basePath, $"{logType}.log");
            if (File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, string.Empty);
            }
        }
    }
} 