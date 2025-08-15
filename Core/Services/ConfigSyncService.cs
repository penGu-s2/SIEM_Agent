using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using SIEM_Agent.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using Timer = System.Timers.Timer;

namespace SIEM_Agent.Core.Services
{
    public class ConfigSyncService
    {
        private readonly string _configUrl;
        private readonly string _localConfigPath;
        private readonly Timer _syncTimer;
        private readonly HttpClient _httpClient;
        private AgentConfigData _currentConfig;

        public event EventHandler<string> OnConfigUpdated;
        public event EventHandler<Exception> OnSyncError;
        public event EventHandler<AgentConfigData> OnConfigReceived;

        public ConfigSyncService(string configUrl, string localConfigPath)
        {
            _configUrl = configUrl;
            _localConfigPath = localConfigPath;
            _httpClient = new HttpClient();
            _syncTimer = new Timer(5 * 60 * 1000); // 5 phút
            _syncTimer.Elapsed += async (sender, e) => await SyncConfigAsync();
        }

        public void Start()
        {
            _syncTimer.Start();
            _ = SyncConfigAsync();
        }

        public void Stop()
        {
            _syncTimer.Stop();
        }

        public AgentConfigData GetCurrentConfig()
        {
            return _currentConfig;
        }

        public async Task SyncConfigAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_configUrl);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var agentConfigResponse = JsonSerializer.Deserialize<AgentConfigResponse>(jsonContent);
                if (agentConfigResponse?.Data == null)
                    throw new Exception("Invalid response format: missing data");
                var agentConfig = agentConfigResponse.Data;
                if (!agentConfig.IsActive)
                    throw new Exception("Configuration is not active");
                if (_currentConfig != null &&
                    _currentConfig.AgentConfigId == agentConfig.AgentConfigId &&
                    _currentConfig.Version == agentConfig.Version)
                {
                    return;
                }
                _currentConfig = agentConfig;
                OnConfigReceived?.Invoke(this, agentConfig);
                await SaveConfigInfoAsync(agentConfig);

                // Xử lý động mọi section
                var hasChanges = await UpdateFluentBitConfigDynamicAndLogAsync(agentConfig.ConfigFluentbit);
                if (hasChanges)
                {
                    SIEM_Agent.Core.FluentBitHelper.StopFluentBit();
                    OnConfigUpdated?.Invoke(this, _localConfigPath);
                }
            }
            catch (Exception ex)
            {
                OnSyncError?.Invoke(this, ex);
            }
        }

        private async Task SaveConfigInfoAsync(AgentConfigData config)
        {
            try
            {
                var configInfoPath = Path.Combine("logs", "agent_config_info.json");
                var configInfo = new
                {
                    LastSyncTime = DateTime.Now,
                    AgentConfigId = config.AgentConfigId,
                    AgentId = config.AgentId,
                    Version = config.Version,
                    IsActive = config.IsActive,
                    CreatedTime = config.CreatedDateTime,
                    UpdatedTime = config.UpdatedDateTime,
                    CreatedBy = config.CreatedBy,
                    ChangeLog = config.ChangeLog
                };
                var jsonString = JsonSerializer.Serialize(configInfo, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configInfoPath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config info: {ex.Message}");
            }
        }

        // Hàm mới: cập nhật config và log lại các thay đổi chi tiết
        private async Task<bool> UpdateFluentBitConfigDynamicAndLogAsync(string jsonConfig)
        {
            try
            {
                var newConfig = JsonSerializer.Deserialize<DynamicFluentBitConfig>(jsonConfig);
                if (newConfig == null)
                    throw new Exception("Failed to deserialize FluentBit config");
                var currentLines = File.Exists(_localConfigPath)
                    ? await File.ReadAllLinesAsync(_localConfigPath)
                    : new string[0];
                var currentSections = ParseSections(currentLines);
                var newSections = ConvertJsonSectionsToBlocks(newConfig.Sections);
                var changes = new List<object>();
                var mergedSections = MergeSectionsKeepOldAndLog(currentSections, newSections, changes);
                bool hasChanges = changes.Count > 0;
                if (hasChanges)
                {
                    var outputLines = SectionsToLines(mergedSections);
                    await File.WriteAllLinesAsync(_localConfigPath, outputLines);
                    // Ghi log JSON
                    var logObj = new
                    {
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        event_type = "config_update",
                        file = _localConfigPath,
                        changes = changes,
                        old_config = string.Join("\n", currentLines),
                        new_config = string.Join("\n", outputLines)
                    };
                    string logPath = Path.Combine("logs", "config_changes.log");
                    using (var sw = new StreamWriter(logPath, true))
                    {
                        sw.WriteLine(JsonSerializer.Serialize(logObj, new JsonSerializerOptions { WriteIndented = false }));
                    }
                }
                return hasChanges;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update FluentBit config: {ex.Message}");
            }
        }

        // Parse file config thành Dictionary<section, List<Dictionary<string, string>>> (block)
        private Dictionary<string, List<Dictionary<string, string>>> ParseSections(string[] lines)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>();
            string currentSection = null;
            var currentBlock = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if (currentSection != null && currentBlock.Count > 0)
                    {
                        if (!result.ContainsKey(currentSection))
                            result[currentSection] = new List<Dictionary<string, string>>();
                        result[currentSection].Add(new Dictionary<string, string>(currentBlock));
                    }
                    currentSection = trimmed.Substring(1, trimmed.Length - 2).ToUpperInvariant();
                    currentBlock.Clear();
                }
                else if (!string.IsNullOrWhiteSpace(trimmed) && currentSection != null)
                {
                    var idx = trimmed.IndexOf(' ');
                    if (idx > 0)
                    {
                        var key = trimmed.Substring(0, idx).Trim();
                        var value = trimmed.Substring(idx + 1).Trim();
                        currentBlock[key] = value;
                    }
                }
            }
            if (currentSection != null && currentBlock.Count > 0)
            {
                if (!result.ContainsKey(currentSection))
                    result[currentSection] = new List<Dictionary<string, string>>();
                result[currentSection].Add(new Dictionary<string, string>(currentBlock));
            }
            return result;
        }

        // Convert JSON sections sang Dictionary<section, List<Dictionary<string, string>>> (block)
        private Dictionary<string, List<Dictionary<string, string>>> ConvertJsonSectionsToBlocks(Dictionary<string, JsonElement> sections)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>();
            foreach (var kv in sections)
            {
                var sectionName = ToFluentBitSectionName(kv.Key);
                var sectionValue = kv.Value;
                if (sectionValue.ValueKind == JsonValueKind.Object)
                {
                    // Single block (ví dụ SERVICE)
                    var block = JsonObjectToDictionary(sectionValue);
                    result[sectionName] = new List<Dictionary<string, string>> { block };
                }
                else if (sectionValue.ValueKind == JsonValueKind.Array)
                {
                    // Nhiều block (ví dụ INPUT, OUTPUT, FILTER, ...)
                    var blocks = new List<Dictionary<string, string>>();
                    foreach (var item in sectionValue.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                            blocks.Add(JsonObjectToDictionary(item));
                    }
                    result[sectionName] = blocks;
                }
            }
            return result;
        }

        // Chuyển JsonElement object sang Dictionary<string, string>
        private Dictionary<string, string> JsonObjectToDictionary(JsonElement obj)
        {
            var dict = new Dictionary<string, string>();
            foreach (var prop in obj.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ToString();
            }
            return dict;
        }

        // Chuyển tên section JSON sang section Fluent Bit (Inputs -> INPUT, Outputs -> OUTPUT, ...)
        private string ToFluentBitSectionName(string jsonSection)
        {
            if (string.IsNullOrWhiteSpace(jsonSection)) return jsonSection;
            var upper = jsonSection.Trim().ToUpperInvariant();
            if (upper.EndsWith("S") && upper.Length > 1)
                upper = upper.Substring(0, upper.Length - 1); // Remove 'S' cuối
            return upper;
        }

        // Merge: chỉ cập nhật hoặc thêm mới block, không xóa block cũ ngoài JSON, đồng thời log lại các thay đổi
        private Dictionary<string, List<Dictionary<string, string>>> MergeSectionsKeepOldAndLog(
            Dictionary<string, List<Dictionary<string, string>>> current,
            Dictionary<string, List<Dictionary<string, string>>> updated,
            List<object> changes)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>();
            var allSections = new HashSet<string>(current.Keys.Concat(updated.Keys));
            foreach (var section in allSections)
            {
                var currentBlocks = current.ContainsKey(section) ? current[section] : new List<Dictionary<string, string>>();
                var updatedBlocks = updated.ContainsKey(section) ? updated[section] : new List<Dictionary<string, string>>();
                var mergedBlocks = new List<Dictionary<string, string>>();
                // Đầu tiên, duyệt block cũ
                foreach (var curBlock in currentBlocks)
                {
                    var updBlock = updatedBlocks.FirstOrDefault(upd => BlockKey(upd) == BlockKey(curBlock));
                    if (updBlock == null)
                    {
                        // Giữ lại block cũ nếu không có trong JSON mới
                        mergedBlocks.Add(new Dictionary<string, string>(curBlock));
                    }
                    else
                    {
                        // So sánh từng trường, cập nhật nếu khác biệt
                        var merged = new Dictionary<string, string>(curBlock);
                        foreach (var kv in updBlock)
                        {
                            if (!merged.ContainsKey(kv.Key) || merged[kv.Key] != kv.Value)
                            {
                                changes.Add(new
                                {
                                    section = section,
                                    block_key = BlockKey(curBlock),
                                    field = kv.Key,
                                    old_value = merged.ContainsKey(kv.Key) ? merged[kv.Key] : null,
                                    new_value = kv.Value
                                });
                                merged[kv.Key] = kv.Value;
                            }
                        }
                        // Nếu block cũ có trường không còn trong block mới, giữ lại (không xóa)
                        mergedBlocks.Add(merged);
                    }
                }
                // Thêm block mới chưa có vào cuối
                foreach (var updBlock in updatedBlocks)
                {
                    if (!currentBlocks.Any(cur => BlockKey(cur) == BlockKey(updBlock)))
                    {
                        changes.Add(new
                        {
                            section = section,
                            block_key = BlockKey(updBlock),
                            field = "__new_block__",
                            old_value = string.Empty,
                            new_value = JsonSerializer.Serialize(updBlock)
                        });
                        mergedBlocks.Add(new Dictionary<string, string>(updBlock));
                    }
                }
                if (mergedBlocks.Count > 0)
                    result[section] = mergedBlocks;
            }
            return result;
        }

        // Tạo key định danh cho block (ưu tiên Name+Tag, Name+Match, nếu không có thì serialize toàn bộ)
        private string BlockKey(Dictionary<string, string> block)
        {
            if (block.ContainsKey("Name") && block.ContainsKey("Tag"))
                return $"{block["Name"]}|{block["Tag"]}";
            if (block.ContainsKey("Name") && block.ContainsKey("Match"))
                return $"{block["Name"]}|{block["Match"]}";
            if (block.ContainsKey("Name"))
                return block["Name"];
            if (block.ContainsKey("Match"))
                return block["Match"];
            return string.Join("|", block.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"));
        }

        // Chuyển Dictionary<section, List<block>> thành List<string> để ghi file
        private List<string> SectionsToLines(Dictionary<string, List<Dictionary<string, string>>> sections)
        {
            var lines = new List<string>();
            foreach (var section in sections)
            {
                foreach (var block in section.Value)
                {
                    lines.Add($"[{section.Key}]");
                    foreach (var kv in block)
                    {
                        lines.Add($"    {kv.Key}    {kv.Value}");
                    }
                    lines.Add("");
                }
            }
            return lines;
        }
    }
} 