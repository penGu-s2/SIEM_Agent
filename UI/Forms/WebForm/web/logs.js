let isCollecting = true;
let currentLogType = 'winlog';

// Khởi tạo tab Logs
function initLogsTab() {
    // Thiết lập datetime mặc định - 2 ngày trước đến hiện tại
    const now = new Date();
    const twoDaysAgo = new Date(now.getTime() - 2 * 24 * 60 * 60 * 1000);
    
    document.getElementById('dtpStart').value = formatDateTimeForInput(twoDaysAgo);
    document.getElementById('dtpEnd').value = formatDateTimeForInput(now);
    
    // Gắn sự kiện cho các nút
    document.getElementById('cmbLogType').onchange = function() {
        currentLogType = this.value;
        updateLogsTableHeaders();
        loadLogs();
    };
    
    document.getElementById('btnLoadLogs').onclick = loadLogs;
    document.getElementById('btnRefreshLogs').onclick = loadLogs;
    document.getElementById('btnClearLogs').onclick = clearLogs;
    document.getElementById('btnToggleCollect').onclick = toggleCollect;
    document.getElementById('btnStartFluentBitConsole').onclick = startFluentBitConsole;
    document.getElementById('btnStartFluentBitOutput').onclick = startFluentBitOutput;
    document.getElementById('btnCheckFluentBitStatus').onclick = checkFluentBitStatus;
    document.getElementById('btnDebugLogFile').onclick = debugLogFile;
    
    // Load log types từ C#
    loadLogTypesFromCSharp();
    
    // Load logs ban đầu
    updateLogsTableHeaders();
    loadLogs();
}

// Load log types từ C#
function loadLogTypesFromCSharp() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'get_log_types' });
    }
}

// Cập nhật dropdown log types từ C#
function updateLogTypesFromCSharp(jsonData) {
    try {
        const logTypes = JSON.parse(jsonData);
        const cmbLogType = document.getElementById('cmbLogType');
        cmbLogType.innerHTML = '';
        
        logTypes.forEach(logType => {
            const option = document.createElement('option');
            // Sử dụng Name của INPUT thay vì tag
            option.value = logType.name || logType.tag;
            option.textContent = logType.displayName || logType.name || logType.tag;
            cmbLogType.appendChild(option);
        });
        
        // Chọn item đầu tiên nếu có
        if (cmbLogType.options.length > 0) {
            cmbLogType.selectedIndex = 0;
            currentLogType = cmbLogType.value;
        }
    } catch (e) {
        console.error('Lỗi dữ liệu log types:', e);
    }
}

// Cập nhật header của bảng log theo loại
function updateLogsTableHeaders() {
    const thead = document.querySelector('#logsTable thead tr');
    thead.innerHTML = '';
    
    // Sử dụng Name của INPUT thay vì tag
    const inputName = currentLogType.toLowerCase();
    
    // Kiểm tra Name có chứa từ khóa để xác định loại log
    if (inputName.includes('winlog') || inputName.includes('windows') || inputName.includes('event')) {
        thead.innerHTML = `
            <th>STT</th>
            <th>Thời gian</th>
            <th>Nguồn</th>
            <th>Cấp độ</th>
            <th>Sự kiện</th>
            <th>Mô tả</th>
        `;
    } else if (inputName.includes('syslog') || inputName.includes('sys')) {
        thead.innerHTML = `
            <th>STT</th>
            <th>Thời gian</th>
            <th>Host</th>
            <th>Facility</th>
            <th>Severity</th>
            <th>Message</th>
        `;
    } else if (inputName.includes('stat') || inputName.includes('performance') || inputName.includes('cpu') || inputName.includes('memory')) {
        thead.innerHTML = `
            <th>STT</th>
            <th>Uptime</th>
            <th>CPU (%)</th>
            <th>Processes</th>
            <th>Threads</th>
            <th>Handles</th>
            <th>RAM Used (MB)</th>
            <th>RAM Total (MB)</th>
        `;
    } else if (inputName.includes('file') || inputName.includes('tail')) {
        thead.innerHTML = `
            <th>STT</th>
            <th>Thời gian</th>
            <th>File</th>
            <th>Level</th>
            <th>Type</th>
            <th>Nội dung</th>
        `;
    } else {
        // Header mặc định cho các loại log khác
        thead.innerHTML = `
            <th>STT</th>
            <th>Thời gian</th>
            <th>Nguồn</th>
            <th>Cấp độ</th>
            <th>Loại</th>
            <th>Nội dung</th>
        `;
    }
}

// Load logs từ C#
function loadLogs() {
    const startTime = document.getElementById('dtpStart').value;
    const endTime = document.getElementById('dtpEnd').value;
    
    // Hiển thị thông báo đang load
    const tbody = document.getElementById('logsTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: blue;">Đang tải logs...</td></tr>';
    
    console.log(`Loading logs for ${currentLogType} from ${startTime} to ${endTime}`);
    
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ 
            action: 'get_logs', 
            logType: currentLogType,
            startTime: startTime,
            endTime: endTime
        });
    }
}

// Clear logs
function clearLogs() {
    if (confirm('Bạn có chắc muốn xóa tất cả log?')) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ 
                action: 'clear_logs', 
                logType: currentLogType 
            });
        }
    }
}

// Toggle collect logs
function toggleCollect() {
    const btn = document.getElementById('btnToggleCollect');
    if (isCollecting) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'stop_fluentbit' });
        }
        btn.textContent = 'Bắt đầu lấy log';
        btn.style.background = '#DC2626';
        isCollecting = false;
    } else {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'start_fluentbit' });
        }
        btn.textContent = 'Dừng lấy log';
        btn.style.background = '#059669';
        isCollecting = true;
    }
}

// Khởi động Fluent Bit với cửa sổ CMD hiển thị
function startFluentBitConsole() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'start_fluentbit_console' });
    }
}

// Khởi động Fluent Bit với output redirect
function startFluentBitOutput() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'start_fluentbit_output' });
    }
}

// Kiểm tra trạng thái Fluent Bit
function checkFluentBitStatus() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'check_fluentbit_status' });
    }
}

// Debug log file
function debugLogFile() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ 
            action: 'debug_log_file',
            logType: currentLogType
        });
    }
}

// Render logs từ C#
function updateLogsFromCSharp(jsonData) {
    try {
        console.log('Nhận dữ liệu log từ C#:', jsonData);
        const logs = JSON.parse(jsonData);
        console.log('Parse JSON thành công, số lượng log:', logs.length);
        
        const tbody = document.getElementById('logsTableBody');
        tbody.innerHTML = '';
        
        if (!logs || logs.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: orange;">Không có dữ liệu log trong khoảng thời gian này</td></tr>';
            return;
        }
        
        let stt = 1;
        let processedCount = 0;
        let errorCount = 0;
        
        logs.forEach((log, index) => {
            if (!log || typeof log !== 'string') {
                console.log(`Bỏ qua log ${index}: không phải string, type: ${typeof log}`);
                return;
            }
            
            try {
                // Debug: Log một vài ký tự đầu tiên để kiểm tra
                if (index < 3) {
                    console.log(`Log ${index} (first 100 chars):`, log.substring(0, 100));
                }
                
                // Kiểm tra xem có phải là JSON fragment không
                const trimmedLog = log.trim();
                if (!trimmedLog.startsWith('{') || !trimmedLog.endsWith('}')) {
                    throw new Error(`JSON fragment detected: starts with "${trimmedLog.substring(0, 10)}..." ends with "...${trimmedLog.substring(Math.max(0, trimmedLog.length - 10))}"`);
                }
                
                const logObj = JSON.parse(log);
                console.log(`Parse log ${index} thành công:`, logObj);
                
                const tr = document.createElement('tr');
                
                // Sử dụng Name của INPUT thay vì tag
                const inputName = currentLogType.toLowerCase();
                
                if (inputName.includes('winlog') || inputName.includes('windows') || inputName.includes('event')) {
                    tr.innerHTML = `
                        <td>${stt++}</td>
                        <td>${logObj.TimeGenerated || ''}</td>
                        <td>${logObj.SourceName || ''}</td>
                        <td>${logObj.EventType || ''}</td>
                        <td>${logObj.EventID || ''}</td>
                        <td>${logObj.Message || ''}</td>
                    `;
                } else if (inputName.includes('syslog') || inputName.includes('sys')) {
                    tr.innerHTML = `
                        <td>${stt++}</td>
                        <td>${logObj.timestamp || ''}</td>
                        <td>${logObj.host || ''}</td>
                        <td>${logObj.facility || ''}</td>
                        <td>${logObj.severity || ''}</td>
                        <td>${logObj.message || ''}</td>
                    `;
                } else if (inputName.includes('stat') || inputName.includes('performance') || inputName.includes('cpu') || inputName.includes('memory')) {
                    const uptime = logObj.uptime_human || '';
                    const cpu = logObj.cpu_utilization || 0;
                    const processes = logObj.processes || 0;
                    const threads = logObj.threads || 0;
                    const handles = logObj.handles || 0;
                    const ramUsed = Math.round((logObj.physical_used || 0) / 1024 / 1024);
                    const ramTotal = Math.round((logObj.physical_total || 0) / 1024 / 1024);
                    
                    tr.innerHTML = `
                        <td>${stt++}</td>
                        <td>${uptime}</td>
                        <td>${cpu}</td>
                        <td>${processes}</td>
                        <td>${threads}</td>
                        <td>${handles}</td>
                        <td>${ramUsed}</td>
                        <td>${ramTotal}</td>
                    `;
                } else if (inputName.includes('file') || inputName.includes('tail')) {
                    tr.innerHTML = `
                        <td>${stt++}</td>
                        <td>${logObj.timestamp || ''}</td>
                        <td>${logObj.file || ''}</td>
                        <td>${logObj.level || ''}</td>
                        <td>${logObj.type || ''}</td>
                        <td>${logObj.message || JSON.stringify(logObj)}</td>
                    `;
                } else {
                    // Render mặc định cho các loại log khác
                    tr.innerHTML = `
                        <td>${stt++}</td>
                        <td>${logObj.timestamp || logObj.TimeGenerated || ''}</td>
                        <td>${logObj.source || logObj.SourceName || ''}</td>
                        <td>${logObj.level || logObj.EventType || ''}</td>
                        <td>${logObj.type || logObj.EventID || ''}</td>
                        <td>${logObj.message || logObj.Message || JSON.stringify(logObj)}</td>
                    `;
                }
                
                tbody.appendChild(tr);
                processedCount++;
                
            } catch (e) {
                console.error(`Lỗi parse log ${index}:`, e);
                console.error('Log content:', log);
                console.error('Log length:', log.length);
                console.error('First 50 chars:', log.substring(0, 50));
                console.error('Last 50 chars:', log.substring(Math.max(0, log.length - 50)));
                
                errorCount++;
                
                // Thêm row lỗi để hiển thị với thông tin chi tiết hơn
                const tr = document.createElement('tr');
                const errorDetails = `
                    <strong>Lỗi parse log:</strong> ${e.message}<br>
                    <strong>Độ dài:</strong> ${log.length} ký tự<br>
                    <strong>Bắt đầu:</strong> ${log.substring(0, 50)}${log.length > 50 ? '...' : ''}<br>
                    <strong>Kết thúc:</strong> ${log.substring(Math.max(0, log.length - 50))}<br>
                    <details style="margin-top: 5px;">
                        <summary>Xem toàn bộ nội dung</summary>
                        <pre style="background: #f5f5f5; padding: 10px; margin: 5px 0; font-size: 12px; overflow-x: auto;">${log}</pre>
                    </details>
                `;
                
                tr.innerHTML = `
                    <td>${stt++}</td>
                    <td colspan="5" style="color: red; text-align: left;">
                        ${errorDetails}
                    </td>
                `;
                tbody.appendChild(tr);
            }
        });
        
        console.log(`Đã xử lý ${processedCount}/${logs.length} log entries thành công, ${errorCount} lỗi`);
        
        if (processedCount === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: red;">Không thể parse được log nào</td></tr>';
        }
        
    } catch (e) {
        console.error('Lỗi dữ liệu logs:', e);
        console.error('Raw data:', jsonData);
        console.error('Raw data type:', typeof jsonData);
        console.error('Raw data length:', jsonData ? jsonData.length : 'null');
        document.getElementById('logsTableBody').innerHTML = '<tr><td colspan="6" style="text-align: center; color: red;">Lỗi tải dữ liệu log</td></tr>';
    }
}

// Format datetime cho input datetime-local
function formatDateTimeForInput(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

// Gắn sự kiện cho tab Logs
document.addEventListener('DOMContentLoaded', function() {
    // Khi click vào tab Logs
    const logsTab = document.querySelector('.sidebar-item[data-tab="logs"]');
    if (logsTab) {
        logsTab.addEventListener('click', function() {
            // Khởi tạo tab Logs khi được click lần đầu
            if (!window.logsTabInitialized) {
                initLogsTab();
                window.logsTabInitialized = true;
            }
        });
    }
});

// Export functions để C# có thể gọi
window.updateLogsFromCSharp = updateLogsFromCSharp;
window.updateLogTypesFromCSharp = updateLogTypesFromCSharp;

// Cập nhật trạng thái Fluent Bit
function updateFluentBitStatus(jsonData) {
    try {
        const status = JSON.parse(jsonData);
        let statusText = `Trạng thái: ${status.isRunning ? 'Đang chạy' : 'Đã dừng'}\n`;
        statusText += `Số process: ${status.processCount}\n`;
        
        if (status.processes && status.processes.length > 0) {
            statusText += '\nChi tiết process:\n';
            status.processes.forEach((proc, index) => {
                statusText += `- Process ${index + 1}:\n`;
                statusText += `  ID: ${proc.id}\n`;
                statusText += `  Thời gian bắt đầu: ${proc.startTime}\n`;
                statusText += `  CPU Time: ${proc.cpuTime.toFixed(2)}s\n`;
                statusText += `  Memory: ${proc.memoryUsage.toFixed(2)} MB\n`;
            });
        }
        
        alert(statusText);
    } catch (e) {
        console.error('Lỗi parse trạng thái Fluent Bit:', e);
        alert('Lỗi khi đọc trạng thái Fluent Bit');
    }
}

window.updateFluentBitStatus = updateFluentBitStatus;

// Hiển thị thông tin debug
function showDebugInfo(jsonData) {
    try {
        const debugInfo = JSON.parse(jsonData);
        let message = '';
        
        if (debugInfo.message) {
            message = debugInfo.message;
        } else if (debugInfo.debugInfo) {
            message = debugInfo.debugInfo.join('\n');
        } else {
            message = 'Không có thông tin debug';
        }
        
        alert('DEBUG INFO:\n\n' + message);
    } catch (e) {
        console.error('Lỗi parse debug info:', e);
        alert('Lỗi khi đọc thông tin debug');
    }
}

window.showDebugInfo = showDebugInfo; 