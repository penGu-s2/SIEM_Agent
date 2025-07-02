using System;
using System.Data.Odbc;
using SIEM_Agent.Core.Entities;
using SIEM_Agent.Core.Interfaces;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIEM_Agent.Core.Collectors
{
    public class OdbcLogCollector : ILogCollector
    {
        public string CollectorType => "odbc";
        public event EventHandler<LogEntry>? OnLogReceived;
        private CancellationTokenSource _cts = new();
        private OdbcConnection? _connection;
        private string _query = string.Empty;

        public void Initialize(string connectionString, string query)
        {
            _connection = new OdbcConnection(connectionString);
            _query = query;
        }

        public async Task StartAsync()
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection not initialized");

            try
            {
                await _connection.OpenAsync();
                while (!_cts.Token.IsCancellationRequested)
                {
                    using var command = new OdbcCommand(_query, _connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        var logEntry = new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            Source = "ODBC",
                            Message = reader[0].ToString() ?? string.Empty
                        };
                        
                        OnLogReceived?.Invoke(this, logEntry);
                    }
                    
                    await Task.Delay(5000, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                // Log error
                throw;
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                }
            }
        }

        public Task StopAsync()
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        public Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime startTime, DateTime endTime) => Task.FromResult<IEnumerable<LogEntry>>(new List<LogEntry>());
        public Task<LogEntry> GetLogByIdAsync(string id) => Task.FromResult<LogEntry>(null);
    }
} 