using System.Text.Json;

namespace SIEM_Agent.Core.Config
{
    public class AgentConfig
    {
        private const string ConfigFile = "agent_config.json";

        public Dictionary<string, Dictionary<string, string>>? Load()
        {
            if (!File.Exists(ConfigFile))
                return null;

            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
        }

        public void Save(Dictionary<string, Dictionary<string, string>> config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
    }
} 