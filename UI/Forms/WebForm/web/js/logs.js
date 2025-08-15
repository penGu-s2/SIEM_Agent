// Logs module - quản lý tab Logs và giao tiếp với C#
// Sử dụng global variables thay vì ES6 modules để tương thích với script loading

// Global variables - sẽ được set sau khi các script khác load
let isCollecting = true;
let currentLogType = 'winlog';
let initialized = false;
let logStructure = {}; // Lưu trữ cấu trúc log cho từng loại

// Khởi tạo tab Logs
function initLogsTab() {
    if (initialized) return;
    initialized = true;
    
    // Thiết lập datetime mặc định - 2 ngày trước đến hiện tại
    const now = new Date();
    const twoDaysAgo = new Date(now.getTime() - 2 * 24 * 60 * 60 * 1000);
    
    const dtStart = document.getElementById('dtpStart');
    const dtEnd = document.getElementById('dtpEnd');
    if (dtStart && dtEnd) {
        dtStart.value = formatDateTimeForInput(twoDaysAgo);
        dtEnd.value = formatDateTimeForInput(now);
    }
    
    // Gắn sự kiện cho các nút
    const cmbLogType = document.getElementById('cmbLogType');
    if (cmbLogType) {
        cmbLogType.onchange = function() {
            currentLogType = this.value;
            updateLogsTableHeaders();
            loadLogs();
            showLogStructure(); // Hiển thị cấu trúc log khi thay đổi loại
        };
    }
    
    const bind = (id, handler) => {
        const el = document.getElementById(id);
        if (el) el.onclick = handler;
    };
    
    bind('btnLoadLogs', loadLogs);
    bind('btnRefreshLogs', loadLogs);
    bind('btnClearLogs', clearLogs);
    bind('btnToggleCollect', toggleCollect);
    bind('btnStartFluentBitConsole', startFluentBitConsole);
    bind('btnStartFluentBitOutput', startFluentBitOutput);
    bind('btnCheckFluentBitStatus', checkFluentBitStatus);
    bind('btnDebugLogFile', debugLogFile);
    
    // Load log types từ C# và logs ban đầu
    loadLogTypesFromCSharp();
    updateLogsTableHeaders();
    loadLogs();
    
    // Hiển thị cấu trúc log mặc định
    showLogStructure();
}

// Load log types từ C#
function loadLogTypesFromCSharp() {
    sendMessageToCSharp('get_log_types');
}

// Cập nhật dropdown log types từ C#
function updateLogTypesFromCSharp(jsonData) {
    try {
        const logTypes = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
        const cmbLogType = document.getElementById('cmbLogType');
        if (!cmbLogType) return;
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
    const theadRow = document.querySelector('#logsTable thead tr');
    if (!theadRow) return;
    theadRow.innerHTML = '';
    
    // Sử dụng Name của INPUT thay vì tag
    const inputName = (currentLogType || '').toLowerCase();
    
    const set = (html) => theadRow.innerHTML = html;
    
    if (inputName.includes('winlog') || inputName.includes('windows') || inputName.includes('event')) {
        set(`
            <th>STT</th>
            <th>Thời gian</th>
            <th>Nguồn</th>
            <th>Cấp độ</th>
            <th>Sự kiện</th>
            <th>Mô tả</th>
        `);
    } else if (inputName.includes('syslog') || inputName.includes('sys')) {
        set(`
            <th>STT</th>
            <th>Thời gian</th>
            <th>Host</th>
            <th>Facility</th>
            <th>Severity</th>
            <th>Message</th>
        `);
    } else if (inputName.includes('stat') || inputName.includes('performance') || inputName.includes('cpu') || inputName.includes('memory')) {
        set(`
            <th>STT</th>
            <th>Uptime</th>
            <th>CPU (%)</th>
            <th>Processes</th>
            <th>Threads</th>
            <th>Handles</th>
            <th>RAM Used (MB)</th>
            <th>RAM Total (MB)</th>
        `);
    } else if (inputName.includes('file') || inputName.includes('tail')) {
        set(`
            <th>STT</th>
            <th>Thời gian</th>
            <th>File</th>
            <th>Level</th>
            <th>Type</th>
            <th>Nội dung</th>
        `);
    } else {
        // Header mặc định cho các loại log khác
        set(`
            <th>STT</th>
            <th>Thời gian</th>
            <th>Nguồn</th>
            <th>Cấp độ</th>
            <th>Loại</th>
            <th>Nội dung</th>
        `);
    }
}

// Load logs từ C#
function loadLogs() {
    const startTime = document.getElementById('dtpStart')?.value;
    const endTime = document.getElementById('dtpEnd')?.value;
    
    // Hiển thị thông báo đang load
    const tbody = document.getElementById('logsTableBody');
    if (tbody) tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: blue;">Đang tải logs...</td></tr>';
    
    // Gửi message với format đúng mà C# mong đợi
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
            action: 'get_logs',
            data: {
                logType: currentLogType,
                startTime: startTime,
                endTime: endTime
            }
        });
    }
}

// Clear logs
function clearLogs() {
    if (confirm('Bạn có chắc muốn xóa tất cả log?')) {
        sendMessageToCSharp('clear_logs', { logType: currentLogType });
    }
}

// Toggle collect logs
function toggleCollect() {
    const btn = document.getElementById('btnToggleCollect');
    if (isCollecting) {
        sendMessageToCSharp('stop_fluentbit');
        if (btn) {
            btn.textContent = 'Bắt đầu lấy log';
            btn.style.background = '#DC2626';
        }
        isCollecting = false;
    } else {
        sendMessageToCSharp('start_fluentbit');
        if (btn) {
            btn.textContent = 'Dừng lấy log';
            btn.style.background = '#059669';
        }
        isCollecting = true;
    }
}

// Khởi động Fluent Bit với cửa sổ CMD hiển thị
function startFluentBitConsole() {
    sendMessageToCSharp('start_fluentbit_console');
}

// Khởi động Fluent Bit với output redirect
function startFluentBitOutput() {
    sendMessageToCSharp('start_fluentbit_output');
}

// Kiểm tra trạng thái Fluent Bit
function checkFluentBitStatus() {
    sendMessageToCSharp('check_fluentbit_status');
}

// Debug log file
function debugLogFile() {
    sendMessageToCSharp('debug_log_file', { logType: currentLogType });
}

// Render logs từ C#
function updateLogsFromCSharp(jsonData) {
    try {
        const logs = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
        const tbody = document.getElementById('logsTableBody');
        if (!tbody) return;
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
                return;
            }
            
            try {
                const trimmedLog = log.trim();
                if (!trimmedLog.startsWith('{') || !trimmedLog.endsWith('}')) {
                    throw new Error('JSON fragment detected');
                }
                
                const logObj = JSON.parse(log);
                const tr = document.createElement('tr');
                
                const inputName = (currentLogType || '').toLowerCase();
                
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
                const tr = document.createElement('tr');
                const errorDetails = `
                    <strong>Lỗi parse log:</strong> ${e.message}<br>
                `;
                tr.innerHTML = `
                    <td>${stt++}</td>
                    <td colspan="5" style="color: red; text-align: left;">${errorDetails}</td>
                `;
                tbody.appendChild(tr);
                errorCount++;
            }
        });
    } catch (e) {
        console.error('Lỗi dữ liệu logs:', e);
        const tbody = document.getElementById('logsTableBody');
        if (tbody) tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: red;">Lỗi tải dữ liệu log</td></tr>';
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

// Hàm hiển thị cấu trúc log cho loại được chọn
function showLogStructure() {
    const logsContent = document.getElementById('tab-logs');
    if (!logsContent) return;
    
    // Tìm hoặc tạo section hiển thị cấu trúc log
    let structureSection = logsContent.querySelector('.log-structure-section');
    if (!structureSection) {
        structureSection = document.createElement('div');
        structureSection.className = 'log-structure-section';
        structureSection.style.cssText = `
            margin: 20px 0;
            padding: 20px;
            background: #1F2937;
            border-radius: 8px;
            border: 1px solid #374151;
        `;
        
        // Chèn vào trước logs table
        const logsTable = document.getElementById('logsTable');
        if (logsTable && logsTable.parentNode) {
            logsTable.parentNode.insertBefore(structureSection, logsTable);
        }
    }
    
    // Cập nhật nội dung cấu trúc log
    updateLogStructureContent(structureSection);
}

// Cập nhật nội dung cấu trúc log
function updateLogStructureContent(structureSection) {
    const logType = currentLogType.toLowerCase();
    
    // Định nghĩa cấu trúc log cho từng loại (không hardcode filePath)
    const logStructures = {
        winlog: {
            title: '📋 Cấu trúc Windows Event Log',
            description: 'Log sự kiện Windows với các trường chuẩn',
            fields: [
                { name: 'EventID', type: 'number', description: 'Mã sự kiện Windows' },
                { name: 'EventType', type: 'string', description: 'Loại sự kiện (Information, Warning, Error)' },
                { name: 'Source', type: 'string', description: 'Nguồn sự kiện' },
                { name: 'TimeCreated', type: 'datetime', description: 'Thời gian tạo sự kiện' },
                { name: 'ComputerName', type: 'string', description: 'Tên máy tính' },
                { name: 'User', type: 'string', description: 'Tài khoản người dùng' },
                { name: 'Message', type: 'string', description: 'Nội dung chi tiết sự kiện' },
                { name: 'Category', type: 'string', description: 'Danh mục sự kiện' },
                { name: 'Keywords', type: 'string', description: 'Từ khóa liên quan' }
            ],
            sample: {
                EventID: 4624,
                EventType: 'Information',
                Source: 'Microsoft-Windows-Security-Auditing',
                TimeCreated: '2024-01-15T10:30:00Z',
                ComputerName: 'DESKTOP-ABC123',
                User: 'DOMAIN\\username',
                Message: 'An account was successfully logged on',
                Category: 'Logon',
                Keywords: 'Audit Success'
            }
        },
        syslog: {
            title: '📋 Cấu trúc Syslog',
            description: 'Log hệ thống Unix/Linux theo chuẩn RFC 5424',
            fields: [
                { name: 'timestamp', type: 'datetime', description: 'Thời gian sự kiện' },
                { name: 'hostname', type: 'string', description: 'Tên máy chủ' },
                { name: 'facility', type: 'string', description: 'Cơ sở hệ thống (kern, user, mail, daemon, auth, syslog, lpr, news, uucp, cron, authpriv, ftp, local0-local7)' },
                { name: 'severity', type: 'string', description: 'Mức độ nghiêm trọng (Emergency, Alert, Critical, Error, Warning, Notice, Informational, Debug)' },
                { name: 'program', type: 'string', description: 'Tên chương trình tạo log' },
                { name: 'pid', type: 'number', description: 'Process ID' },
                { name: 'message', type: 'string', description: 'Nội dung log message' },
                { name: 'structured_data', type: 'object', description: 'Dữ liệu có cấu trúc (SD-ID)' }
            ],
            sample: {
                timestamp: '2024-01-15T10:30:00Z',
                hostname: 'server01',
                facility: 'daemon',
                severity: 'Info',
                program: 'sshd',
                pid: 12345,
                message: 'Accepted password for user from 192.168.1.100 port 12345 ssh2',
                structured_data: '[exampleSDID@32473 iut="3" eventSource="Application" eventID="1011"]'
            }
        },
        http: {
            title: '📋 Cấu trúc HTTP Access Log',
            description: 'Log truy cập HTTP/Web server',
            fields: [
                { name: 'timestamp', type: 'datetime', description: 'Thời gian request' },
                { name: 'remote_addr', type: 'string', description: 'IP address của client' },
                { name: 'remote_user', type: 'string', description: 'Tên người dùng (nếu có auth)' },
                { name: 'request_method', type: 'string', description: 'HTTP method (GET, POST, PUT, DELETE)' },
                { name: 'request_uri', type: 'string', description: 'URI được request' },
                { name: 'http_version', type: 'string', description: 'Phiên bản HTTP' },
                { name: 'status_code', type: 'number', description: 'HTTP status code' },
                { name: 'response_size', type: 'number', description: 'Kích thước response (bytes)' },
                { name: 'user_agent', type: 'string', description: 'User-Agent header' },
                { name: 'referer', type: 'string', description: 'Referer header' },
                { name: 'response_time', type: 'number', description: 'Thời gian xử lý request (ms)' }
            ],
            sample: {
                timestamp: '2024-01-15T10:30:00Z',
                remote_addr: '192.168.1.100',
                remote_user: 'admin',
                request_method: 'GET',
                request_uri: '/api/users',
                http_version: 'HTTP/1.1',
                status_code: 200,
                response_size: 1024,
                user_agent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
                referer: 'https://example.com/dashboard',
                response_time: 150
            }
        },
        tail: {
            title: '📋 Cấu trúc Tail Log (Custom)',
            description: 'Log từ file tùy chỉnh, cấu trúc phụ thuộc vào format',
            fields: [
                { name: 'timestamp', type: 'datetime', description: 'Thời gian log entry' },
                { name: 'level', type: 'string', description: 'Mức độ log (DEBUG, INFO, WARN, ERROR)' },
                { name: 'logger', type: 'string', description: 'Tên logger/module' },
                { name: 'message', type: 'string', description: 'Nội dung log message' },
                { name: 'thread', type: 'string', description: 'Tên thread (nếu có)' },
                { name: 'class', type: 'string', description: 'Tên class (nếu có)' },
                { name: 'line', type: 'number', description: 'Số dòng code (nếu có)' },
                { name: 'custom_fields', type: 'object', description: 'Các trường tùy chỉnh khác' }
            ],
            sample: {
                timestamp: '2024-01-15T10:30:00.123Z',
                level: 'INFO',
                logger: 'com.example.UserService',
                message: 'User login successful',
                thread: 'main',
                class: 'UserService.java',
                line: 45,
                custom_fields: {
                    user_id: '12345',
                    session_id: 'abc123def456',
                    ip_address: '192.168.1.100'
                }
            }
        }
    };
    
    const structure = logStructures[logType] || {
        title: '📋 Cấu trúc Log',
        description: 'Cấu trúc log cho loại: ' + logType,
        fields: [],
        sample: {}
    };
    
    // Tạo HTML cho cấu trúc log
    let html = `
        <div style="margin-bottom: 20px;">
            <h3 style="color: #F9FAFB; margin: 0 0 8px 0; font-size: 18px;">${structure.title}</h3>
            <p style="color: #D1D5DB; margin: 0 0 16px 0; font-size: 14px;">${structure.description}</p>
        </div>
    `;
    
    // Thêm thông tin về file log - LẤY ĐỘNG TỪ CẤU HÌNH
    const logFile = getLogFilePath(logType);
    html += `
        <div style="margin-bottom: 20px; padding: 12px; background: #374151; border-radius: 6px; border-left: 4px solid #3B82F6;">
            <div style="color: #F9FAFB; font-weight: 600; margin-bottom: 4px;">📁 File Log:</div>
            <div id="logFilePath-${logType}" style="color: #D1D5DB; font-family: monospace; font-size: 13px;">${logFile}</div>
        </div>
    `;
    
    // Thêm danh sách các trường
    if (structure.fields.length > 0) {
        html += `
            <div style="margin-bottom: 20px;">
                <h4 style="color: #F9FAFB; margin: 0 0 12px 0; font-size: 16px;">🔍 Các trường dữ liệu:</h4>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 12px;">
        `;
        
        structure.fields.forEach(field => {
            html += `
                <div style="padding: 12px; background: #374151; border-radius: 6px; border: 1px solid #4B5563;">
                    <div style="color: #F9FAFB; font-weight: 600; margin-bottom: 4px;">${field.name}</div>
                    <div style="color: #9CA3AF; font-size: 12px; margin-bottom: 4px;">Type: ${field.type}</div>
                    <div style="color: #D1D5DB; font-size: 13px;">${field.description}</div>
                </div>
            `;
        });
        
        html += `
                </div>
            </div>
        `;
    }
    
    // Thêm ví dụ dữ liệu
    if (Object.keys(structure.sample).length > 0) {
        html += `
            <div style="margin-bottom: 20px;">
                <h4 style="color: #F9FAFB; margin: 0 0 12px 0; font-size: 16px;">📝 Ví dụ dữ liệu:</h4>
                <div style="padding: 16px; background: #111827; border-radius: 6px; border: 1px solid #374151; overflow-x: auto;">
                    <pre style="color: #D1D5DB; font-family: 'Consolas', 'Monaco', monospace; font-size: 13px; margin: 0; white-space: pre-wrap;">${JSON.stringify(structure.sample, null, 2)}</pre>
                </div>
            </div>
        `;
    }
    
    // Thêm nút để xem file log thực tế
    html += `
        <div style="text-align: center;">
            <button onclick="viewActualLogFile('${logType}')" style="
                padding: 8px 16px;
                background: #3B82F6;
                color: white;
                border: none;
                border-radius: 6px;
                cursor: pointer;
                font-size: 14px;
                font-weight: 600;
            ">
                👁️ Xem file log thực tế
            </button>
        </div>
    `;
    
    structureSection.innerHTML = html;
}

// Hàm lấy đường dẫn file log từ cấu hình thực tế
// Cách hoạt động:
// 1. JavaScript gửi request 'get_log_file_path' tới C#
// 2. C# parse fluent-bit.conf, tìm [OUTPUT] block với Match = logType
// 3. C# trả về đường dẫn thực tế (Path + File)
// 4. JavaScript cập nhật UI và cache
function getLogFilePath(logType) {
    const logTypeLower = logType.toLowerCase();
    
    // Kiểm tra cache trước (nếu C# đã gửi đường dẫn)
    if (window.logFilePaths && window.logFilePaths[logTypeLower]) {
        return window.logFilePaths[logTypeLower];
    }
    
    // Nếu chưa có trong cache, gửi message tới C# để lấy đường dẫn
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
            action: 'get_log_file_path',
            data: { logType: logTypeLower }
        });
    }
    
    // Trả về placeholder - C# sẽ cập nhật đường dẫn thực tế
    return 'Đang tải đường dẫn...';
}

// Hàm xem file log thực tế
function viewActualLogFile(logType) {
    // Lấy đường dẫn động từ cấu hình fluent-bit.conf
    const logFile = getLogFilePath(logType);
    
    // Gửi message tới C# để đọc file log
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
            action: 'view_log_file',
            data: { 
                logType: logType, 
                filePath: logFile 
            }
        });
    }
    
    showNotification(`Đang đọc file log: ${logFile}`, 'info');
}

// Cập nhật trạng thái Fluent Bit
function updateFluentBitStatus(jsonData) {
    try {
        const status = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
        let statusText = `Trạng thái: ${status.isRunning ? 'Đang chạy' : 'Đã dừng'}\n`;
        statusText += `Số process: ${status.processCount}\n`;
        
        if (status.processes && status.processes.length > 0) {
            statusText += '\nChi tiết process:\n';
            status.processes.forEach((proc, index) => {
                statusText += `- Process ${index + 1}:\n`;
                statusText += `  ID: ${proc.id}\n`;
                statusText += `  Thời gian bắt đầu: ${proc.startTime}\n`;
                statusText += `  CPU Time: ${proc.cpuTime?.toFixed ? proc.cpuTime.toFixed(2) : proc.cpuTime}s\n`;
                statusText += `  Memory: ${proc.memoryUsage?.toFixed ? proc.memoryUsage.toFixed(2) : proc.memoryUsage} MB\n`;
            });
        }
        
        alert(statusText);
    } catch (e) {
        console.error('Lỗi parse trạng thái Fluent Bit:', e);
        alert('Lỗi khi đọc trạng thái Fluent Bit');
    }
}

// Hiển thị thông tin debug
function showDebugInfo(jsonData) {
    try {
        const debugInfo = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
        let message = '';
        
        if (debugInfo.message) {
            message = debugInfo.message;
        } else if (debugInfo.debugInfo) {
            message = Array.isArray(debugInfo.debugInfo) ? debugInfo.debugInfo.join('\n') : String(debugInfo.debugInfo);
        } else {
            message = 'Không có thông tin debug';
        }
        
        alert('DEBUG INFO:\n\n' + message);
    } catch (e) {
        console.error('Lỗi parse debug info:', e);
        alert('Lỗi khi đọc thông tin debug');
    }
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
window.updateFluentBitStatus = updateFluentBitStatus;
window.showDebugInfo = showDebugInfo;

// Thêm các function wrapper để main.js có thể gọi
window.updateLogsFromCSharpForLogsModule = updateLogsFromCSharp;
window.updateLogTypesFromCSharpForLogsModule = updateLogTypesFromCSharp;
window.updateFluentBitStatusFromLogsModule = updateFluentBitStatus;

// Expose function để C# cập nhật đường dẫn log file
window.updateLogFilePathFromCSharpForLogsModule = function(logType, filePath) {
    console.log(`📁 Cập nhật đường dẫn file log cho ${logType}: ${filePath}`);
    
    // Lưu đường dẫn vào cache để sử dụng sau này
    if (!window.logFilePaths) {
        window.logFilePaths = {};
    }
    window.logFilePaths[logType.toLowerCase()] = filePath;
    
    // Cập nhật UI động - thay đổi text trong element hiện tại
    const logFilePathElement = document.getElementById(`logFilePath-${logType}`);
    if (logFilePathElement) {
        logFilePathElement.textContent = filePath;
        logFilePathElement.style.color = '#10B981'; // Màu xanh khi đã load xong
    }
    
    // Cập nhật lại cấu trúc log nếu cần
    if (typeof showLogStructure === 'function') {
        showLogStructure();
    }
};
