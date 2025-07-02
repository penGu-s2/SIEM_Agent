using System.Threading.Tasks;

namespace SIEM_Agent.Core.Interfaces
{
    public interface IConfigRepository
    {
        Task<string> GetConfigAsync(string logType);
        Task SaveConfigAsync(string logType, string config);
    }
} 