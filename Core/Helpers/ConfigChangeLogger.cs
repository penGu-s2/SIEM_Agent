using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SIEM_Agent.Core.Helpers
{
    public static class ConfigChangeLogger
    {
        public static bool CompareAndLogChanges(string oldConfigPath, string newConfigPath, string logPath)
        {
            if (!File.Exists(oldConfigPath) || !File.Exists(newConfigPath))
                return false;

            var oldJson = JsonDocument.Parse(File.ReadAllText(oldConfigPath)).RootElement;
            var newJson = JsonDocument.Parse(File.ReadAllText(newConfigPath)).RootElement;

            var changes = new List<string>();
            CompareElements("", oldJson, newJson, changes);

            if (changes.Count > 0)
            {
                using (var sw = new StreamWriter(logPath, true))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Các thay đổi cấu hình:");
                    foreach (var change in changes)
                        sw.WriteLine(change);
                    sw.WriteLine();
                }
                return true;
            }
            return false;
        }

        private static void CompareElements(string path, JsonElement oldElem, JsonElement newElem, List<string> changes)
        {
            if (oldElem.ValueKind == JsonValueKind.Object && newElem.ValueKind == JsonValueKind.Object)
            {
                var oldProps = new HashSet<string>();
                foreach (var prop in oldElem.EnumerateObject())
                    oldProps.Add(prop.Name);
                foreach (var prop in newElem.EnumerateObject())
                {
                    var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                    if (oldElem.TryGetProperty(prop.Name, out var oldProp))
                        CompareElements(propPath, oldProp, prop.Value, changes);
                    else
                        changes.Add($"Thêm mới: {propPath} = {prop.Value}");
                }
                foreach (var prop in oldElem.EnumerateObject())
                {
                    if (!newElem.TryGetProperty(prop.Name, out _))
                        changes.Add($"Đã xóa: {path}.{prop.Name} (giá trị cũ: {prop.Value})");
                }
            }
            else if (!JsonElementEquals(oldElem, newElem))
            {
                changes.Add($"Thay đổi: {path} từ '{oldElem}' thành '{newElem}'");
            }
        }

        private static bool JsonElementEquals(JsonElement a, JsonElement b)
        {
            return a.ToString() == b.ToString();
        }
    }
} 