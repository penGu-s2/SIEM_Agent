using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SIEM_Agent.Core.Entities
{
    public class AgentConfigResponse
    {
        [JsonPropertyName("data")]
        public AgentConfigData Data { get; set; }
    }

    public class AgentConfigData
    {
        [JsonPropertyName("agent_config_id")]
        public string AgentConfigId { get; set; }
        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }
        [JsonPropertyName("version")]
        public int Version { get; set; }
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
        [JsonPropertyName("config_fluentbit")]
        public string ConfigFluentbit { get; set; }
        [JsonPropertyName("config_custom")]
        public string ConfigCustom { get; set; }
        [JsonPropertyName("created_time")]
        public long CreatedTime { get; set; }
        [JsonPropertyName("updated_time")]
        public long UpdatedTime { get; set; }
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; }
        [JsonPropertyName("change_log")]
        public string ChangeLog { get; set; }

        public DateTime CreatedDateTime => DateTimeOffset.FromUnixTimeSeconds(CreatedTime).DateTime;
        public DateTime UpdatedDateTime => DateTimeOffset.FromUnixTimeSeconds(UpdatedTime).DateTime;
    }

    // Model động cho Fluent Bit config
    public class DynamicFluentBitConfig
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Sections { get; set; } = new();
    }
} 