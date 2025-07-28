using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;

namespace SIEM_Agent.Core
{
    public static class FluentBitHelper
    {
        private static Process? _fluentBitProcess;
        //private const string FLUENT_BIT_PATH = @"C:\Program Files\fluent-bit\bin\fluent-bit.exe";
        private static string GetExeDir() => AppDomain.CurrentDomain.BaseDirectory;
        private static string GetFluentBitExePath() => Path.Combine(GetExeDir(), "FluentBit", "fluent-bit.exe");
        private static string GetConfigPath() => Path.Combine(GetExeDir(), "fluent-bit.conf");
        public static void RestartFluentBitWithNotify()
        {
            try
            {
                var fluentBitExe = GetFluentBitExePath();
                if (!File.Exists(fluentBitExe))
                {
                    throw new Exception("Không tìm thấy Fluent Bit. Vui lòng cài đặt Fluent Bit vào đường dẫn: " + fluentBitExe);
                }
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    throw new Exception("Không tìm thấy file cấu hình fluent-bit.conf tại: " + configPath);
                }
                StopFluentBit();
                var startInfo = new ProcessStartInfo
                {
                    FileName = fluentBitExe,
                    Arguments = $"-c \"{configPath}\"",
                    UseShellExecute = false, // Để ẩn cửa sổ
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                _fluentBitProcess = Process.Start(startInfo);
                if (_fluentBitProcess == null)
                {
                    throw new Exception("Không thể khởi động Fluent Bit");
                }
                // MessageBox.Show("Fluent Bit đã được khởi động để thu thập log!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi khởi động Fluent Bit: {ex.Message}");
            }
        }
        
        public static void StartFluentBit()
        {
            try
            {
                var fluentBitExe = GetFluentBitExePath();
                if (!File.Exists(fluentBitExe))
                {
                    throw new Exception("Không tìm thấy Fluent Bit. Vui lòng cài đặt Fluent Bit vào đường dẫn: " + fluentBitExe);
                }
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    throw new Exception("Không tìm thấy file cấu hình fluent-bit.conf tại: " + configPath);
                }
                var startInfo = new ProcessStartInfo
                {
                    FileName = fluentBitExe,
                    Arguments = $"-c \"{configPath}\"",
                    UseShellExecute = false, // Để ẩn cửa sổ
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                _fluentBitProcess = Process.Start(startInfo);
                if (_fluentBitProcess == null)
                {
                    throw new Exception("Không thể khởi động Fluent Bit");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi khởi động Fluent Bit: {ex.Message}");
            }
        }

        // Thêm method mới để khởi động Fluent Bit với cửa sổ CMD hiển thị
        public static void StartFluentBitWithConsole()
        {
            try
            {
                var fluentBitExe = GetFluentBitExePath();
                if (!File.Exists(fluentBitExe))
                {
                    throw new Exception("Không tìm thấy Fluent Bit. Vui lòng cài đặt Fluent Bit vào đường dẫn: " + fluentBitExe);
                }
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    throw new Exception("Không tìm thấy file cấu hình fluent-bit.conf tại: " + configPath);
                }
                
                // Dừng process cũ nếu có
                StopFluentBit();
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = fluentBitExe,
                    Arguments = $"-c \"{configPath}\"",
                    UseShellExecute = true, // Hiển thị cửa sổ
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                _fluentBitProcess = Process.Start(startInfo);
                if (_fluentBitProcess == null)
                {
                    throw new Exception("Không thể khởi động Fluent Bit");
                }
                
                MessageBox.Show("Fluent Bit đã được khởi động với cửa sổ CMD hiển thị!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi khởi động Fluent Bit: {ex.Message}");
            }
        }

        // Thêm method để khởi động Fluent Bit với output redirect để đọc log
        public static void StartFluentBitWithOutputRedirect()
        {
            try
            {
                var fluentBitExe = GetFluentBitExePath();
                if (!File.Exists(fluentBitExe))
                {
                    throw new Exception("Không tìm thấy Fluent Bit. Vui lòng cài đặt Fluent Bit vào đường dẫn: " + fluentBitExe);
                }
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    throw new Exception("Không tìm thấy file cấu hình fluent-bit.conf tại: " + configPath);
                }
                
                // Dừng process cũ nếu có
                StopFluentBit();
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = fluentBitExe,
                    Arguments = $"-c \"{configPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                
                _fluentBitProcess = new Process();
                _fluentBitProcess.StartInfo = startInfo;
                _fluentBitProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // Ghi log vào file hoặc hiển thị
                        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Data}";
                        File.AppendAllText(Path.Combine(GetExeDir(), "fluent-bit-console.log"), logMessage + Environment.NewLine);
                    }
                };
                _fluentBitProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {e.Data}";
                        File.AppendAllText(Path.Combine(GetExeDir(), "fluent-bit-console.log"), errorMessage + Environment.NewLine);
                    }
                };
                
                _fluentBitProcess.Start();
                _fluentBitProcess.BeginOutputReadLine();
                _fluentBitProcess.BeginErrorReadLine();
                
                if (_fluentBitProcess == null)
                {
                    throw new Exception("Không thể khởi động Fluent Bit");
                }
                
                MessageBox.Show("Fluent Bit đã được khởi động với output redirect!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi khởi động Fluent Bit: {ex.Message}");
            }
        }
        
        public static void StopFluentBit()
        {
            try
            {
                // Dừng tất cả process fluent-bit đang chạy
                foreach (var process in Process.GetProcessesByName("fluent-bit"))
                {
                    process.Kill();
                    process.WaitForExit();
                }

                if (_fluentBitProcess != null && !_fluentBitProcess.HasExited)
                {
                    _fluentBitProcess.Kill();
                    _fluentBitProcess.WaitForExit();
                    _fluentBitProcess.Dispose();
                    _fluentBitProcess = null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi dừng Fluent Bit: {ex.Message}");
            }
        }

        // Thêm method để kiểm tra trạng thái Fluent Bit
        public static bool IsFluentBitRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName("fluent-bit");
                return processes != null && processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        // Thêm method để lấy thông tin process Fluent Bit
        public static Process[] GetFluentBitProcesses()
        {
            try
            {
                return Process.GetProcessesByName("fluent-bit");
            }
            catch
            {
                return new Process[0];
            }
        }
    }
}