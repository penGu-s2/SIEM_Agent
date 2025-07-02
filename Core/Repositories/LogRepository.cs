using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Globalization;
using SIEM_Agent.Core.Entities;
using SIEM_Agent.Core.Interfaces;
using SIEM_Agent.Core.Helpers;
using System.Text.Json;

namespace SIEM_Agent.Core.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly string _basePath;
        private readonly string _aesKey;

        public LogRepository(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
            // Đọc key từ agent_config.json
            try
            {
                var configText = File.ReadAllText("agent_config.json");
                using var doc = JsonDocument.Parse(configText);
                if (doc.RootElement.TryGetProperty("aes_key", out var keyProp))
                {
                    _aesKey = keyProp.GetString() ?? "randonkey1234598999";
                }
                else
                {
                    _aesKey = "randonkey1234598999";
                }
            }
            catch
            {
                _aesKey = "randonkey1234598999";
            }
        }

        public async Task SaveLogAsync(string logType, string message)
        {
            var filePath = Path.Combine(_basePath, $"{logType}.log");
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{message}";
            var encrypted = CryptoHelper.Encrypt(logEntry, _aesKey);
            await File.AppendAllTextAsync(filePath, encrypted + Environment.NewLine);
        }

        public async Task<IEnumerable<string>> GetLogsAsync(string logType, DateTime startTime, DateTime endTime)
        {
            // Map các giá trị logType về đúng tên file log
            string fileName = logType;
            if (fileName.Equals("winstat", StringComparison.OrdinalIgnoreCase) || fileName.Equals("win_stat", StringComparison.OrdinalIgnoreCase))
                fileName = "win_stat";
            var filePath = Path.Combine(_basePath, $"{fileName}.log");
            if (!File.Exists(filePath))
            {
                return Enumerable.Empty<string>();
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            var plainLines = lines.Where(x => !string.IsNullOrWhiteSpace(x));

            return plainLines.Where(line =>
            {
                try
                {
                    // Parse JSON
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    // Lấy trường thời gian phù hợp
                    string timeField = logType == "winlog" ? "TimeGenerated" : "timestamp";
                    if (root.TryGetProperty(timeField, out var timeProp))
                    {
                        var timeStr = timeProp.GetString();
                        if (DateTime.TryParse(timeStr, out DateTime logTime))
                        {
                            return logTime >= startTime && logTime <= endTime;
                        }
                    }
                    // Nếu là win_stat thì không lọc theo thời gian, luôn trả về true
                    if (fileName == "win_stat")
                        return true;
                    return false;
                }
                catch
                {
                    return false;
                }
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

        public async Task<IEnumerable<LogEntry>> GetLogsByTypeAsync(string logType, DateTime startTime, DateTime endTime)
        {
            var filePath = Path.Combine(_basePath, $"{logType}.log");
            if (!File.Exists(filePath))
            {
                return Enumerable.Empty<LogEntry>();
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            return lines.Select(line =>
            {
                var parts = line.Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 && DateTime.TryParseExact(string.Join(" ", parts.Take(5)), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime logTime))
                {
                    return new LogEntry
                    {
                        LogType = logType,
                        Timestamp = logTime,
                        Message = line.Substring(20).Trim()
                    };
                }
                return null;
            }).Where(log => log != null && log.Timestamp >= startTime && log.Timestamp <= endTime);
        }

        public async Task<LogEntry> GetLogByIdAsync(string id)
        {
            var filePath = Path.Combine(_basePath, $"{id}.log");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length > 0)
            {
                var parts = lines[0].Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 && DateTime.TryParseExact(string.Join(" ", parts.Take(5)), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime logTime))
                {
                    return new LogEntry
                    {
                        Id = id,
                        LogType = id.Split('.').First(),
                        Timestamp = logTime,
                        Message = lines[0].Substring(20).Trim()
                    };
                }
            }
            return null;
        }

        public async Task DeleteLogsByTypeAsync(string logType, DateTime startTime, DateTime endTime)
        {
            var filePath = Path.Combine(_basePath, $"{logType}.log");
            if (File.Exists(filePath))
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                var filteredLines = lines.Where(line =>
                {
                    if (DateTime.TryParse(line.Substring(0, 19), out DateTime logTime))
                    {
                        return logTime < startTime || logTime > endTime;
                    }
                    return true;
                });
                await File.WriteAllLinesAsync(filePath, filteredLines);
            }
        }
    }
} 