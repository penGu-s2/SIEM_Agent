using System;
using System.IO;
using System.Threading.Tasks;
using SIEM_Agent.Core.Interfaces;

namespace SIEM_Agent.Core.Repositories
{
    public class ConfigRepository : IConfigRepository
    {
        private readonly string _configBasePath;

        public ConfigRepository(string configBasePath)
        {
            _configBasePath = configBasePath;
            if (!Directory.Exists(_configBasePath))
            {
                Directory.CreateDirectory(_configBasePath);
            }
        }

        public async Task<string> GetConfigAsync(string logType)
        {
            string configPath = Path.Combine(_configBasePath, $"{logType}.conf");
            if (!File.Exists(configPath))
            {
                return string.Empty;
            }
            return await File.ReadAllTextAsync(configPath);
        }

        public async Task SaveConfigAsync(string logType, string config)
        {
            string configPath = Path.Combine(_configBasePath, $"{logType}.conf");
            await File.WriteAllTextAsync(configPath, config);
        }
    }
} 