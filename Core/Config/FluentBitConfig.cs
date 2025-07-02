using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace SIEM_Agent.Core.Config
{
    public class FluentBitConfig
    {
        private readonly string _configPath;
        private Dictionary<string, Dictionary<string, string>> _config;
        private string _configContent;

        public FluentBitConfig(string configPath)
        {
            _configPath = configPath;
            _config = new Dictionary<string, Dictionary<string, string>>();
            _configContent = File.Exists(_configPath) ? File.ReadAllText(_configPath) : "";
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var options = new JsonSerializerOptions
                        {
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };
                        
                        var loadedConfig = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, options);
                        if (loadedConfig != null)
                        {
                            _config = loadedConfig;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Nếu có lỗi khi đọc file JSON, sử dụng cấu hình mặc định
                _config = new Dictionary<string, Dictionary<string, string>>();
            }
        }

        public List<string> GetAllInputNames()
        {
            var names = new List<string>();
            if (!File.Exists(_configPath)) return names;
            var lines = File.ReadAllLines(_configPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[INPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int j = i + 1;
                    while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                    {
                        if (lines[j].Trim().StartsWith("Name"))
                        {
                            var parts = lines[j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1)
                                names.Add(parts[1].Trim());
                        }
                        j++;
                    }
                }
            }
            return names;
        }

        public Dictionary<string, string> GetInputConfig(string logType)
        {
            if (!File.Exists(_configPath)) return new Dictionary<string, string>();
            var lines = File.ReadAllLines(_configPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[INPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int j = i + 1;
                    bool found = false;
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                    {
                        var line = lines[j].Trim();
                        if (line.StartsWith("Name") && line.Contains(logType))
                            found = true;
                        var match = System.Text.RegularExpressions.Regex.Match(line, "^(\\w+)\\s+(.*)$");
                        if (match.Success)
                            dict[match.Groups[1].Value] = match.Groups[2].Value;
                        j++;
                    }
                    if (found) return dict;
                }
            }
            return new Dictionary<string, string>();
        }

        public void UpdateInputConfig(string logType, Dictionary<string, string> config)
        {
            _config[logType] = config;
            UpdateInputBlock(logType, config);
            SaveConfig();
        }

        // Hàm cập nhật block [INPUT] trong file cấu hình Fluent Bit
        private void UpdateInputBlock(string logType, Dictionary<string, string> config)
        {
            string filePath = _configPath;
            if (!File.Exists(filePath)) return;
            var lines = File.ReadAllLines(filePath).ToList();
            int startIdx = -1, endIdx = -1;
            string nameValue = config.ContainsKey("Name") ? config["Name"] : logType;
            // Tìm block [INPUT] đúng loại log
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Equals("[INPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int j = i + 1;
                    bool found = false;
                    while (j < lines.Count && !lines[j].Trim().StartsWith("["))
                    {
                        if (lines[j].Trim().StartsWith("Name") && lines[j].Contains(nameValue))
                        {
                            startIdx = i;
                            // Tìm endIdx
                            endIdx = j;
                            while (endIdx + 1 < lines.Count && !lines[endIdx + 1].Trim().StartsWith("["))
                                endIdx++;
                            found = true;
                            break;
                        }
                        j++;
                    }
                    if (found) break;
                }
            }
            if (startIdx == -1 || endIdx == -1) return;
            // Đọc các dòng cũ trong block
            var oldBlock = lines.GetRange(startIdx, endIdx - startIdx + 1);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in oldBlock)
            {
                var match = System.Text.RegularExpressions.Regex.Match(line.Trim(), "^(\\w+)\\s+(.*)$");
                if (match.Success)
                {
                    dict[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }
            // Cập nhật các key mới
            foreach (var kv in config)
            {
                dict[kv.Key] = kv.Value;
            }
            // Tạo block mới
            var newBlock = new List<string> { "[INPUT]" };
            foreach (var kv in dict)
            {
                newBlock.Add($"    {kv.Key}         {kv.Value}");
            }
            // Thay thế block cũ
            lines.RemoveRange(startIdx, endIdx - startIdx + 1);
            lines.InsertRange(startIdx, newBlock);
            File.WriteAllLines(filePath, lines);
        }

        public Dictionary<string, string> GetOutputConfig()
        {
            var config = new Dictionary<string, string>();
            var pattern = @"\[OUTPUT\][\s\S]*?(?=\[|$)";
            var match = Regex.Match(_configContent, pattern);

            if (match.Success)
            {
                var lines = match.Value.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(new[] { ' ' }, 2);
                    if (parts.Length == 2)
                    {
                        config[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            return config;
        }

        public void UpdateOutputConfig(Dictionary<string, string> config)
        {
            var pattern = @"\[OUTPUT\][\s\S]*?(?=\[|$)";
            var newConfig = "[OUTPUT]\n";
            
            foreach (var kvp in config)
            {
                newConfig += $"    {kvp.Key}         {kvp.Value}\n";
            }

            if (Regex.IsMatch(_configContent, pattern))
            {
                _configContent = Regex.Replace(_configContent, pattern, newConfig);
            }
            else
            {
                _configContent += "\n" + newConfig;
            }

            SaveConfig();
        }

        private void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    AllowTrailingCommas = true
                };
                var json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configPath + ".json", json); // Lưu json sang file phụ
            }
            catch (Exception ex)
            {
                // Log error or handle it appropriately
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        // Trả về danh sách tất cả các block [INPUT] trong file cấu hình
        public List<Dictionary<string, string>> GetAllInputBlocks()
        {
            var result = new List<Dictionary<string, string>>();
            if (!File.Exists(_configPath)) return result;
            var lines = File.ReadAllLines(_configPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[INPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int j = i + 1;
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                    {
                        var line = lines[j].Trim();
                        var match = System.Text.RegularExpressions.Regex.Match(line, "^(\\w+)\\s+(.*)$");
                        if (match.Success)
                            dict[match.Groups[1].Value] = match.Groups[2].Value;
                        j++;
                    }
                    if (dict.Count > 0)
                        result.Add(dict);
                }
            }
            return result;
        }
    }
} 