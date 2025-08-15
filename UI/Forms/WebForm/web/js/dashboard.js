// Dashboard.js - Quản lý giao diện dashboard và collectors
// Biến global để lưu trữ dữ liệu - KHỞI TẠO TRƯỚC
let selectedCollectorIndex = null;
let collectors = []; // Lưu trữ dữ liệu collector

// Expose biến global để các modules khác có thể truy cập
window.selectedCollectorIndex = selectedCollectorIndex;
window.collectors = collectors;

console.log('🚀 Dashboard.js loaded - Global variables initialized');
console.log('- selectedCollectorIndex:', selectedCollectorIndex);
console.log('- collectors:', collectors);
console.log('- window.selectedCollectorIndex:', window.selectedCollectorIndex);
console.log('- window.collectors:', window.collectors);

function renderCollectors(data) {
    console.log('🎯 Rendering collectors:', data);
    console.log('🎯 Data length:', data ? data.length : 'null');
    console.log('🎯 Data type:', Array.isArray(data) ? 'Array' : typeof data);
    
    collectors = data; // Lưu trữ dữ liệu
    window.collectors = data; // Cập nhật biến global
    
    const tbody = document.getElementById('collector-table-body');
    
    console.log('🔍 Looking for collector-table-body element...');
    console.log('🔍 tbody element:', tbody);
    
    if (!tbody) {
        console.error('❌ Không tìm thấy collector-table-body');
        console.error('❌ Available elements with similar names:');
        document.querySelectorAll('[id*="collector"]').forEach(el => {
            console.error(`  - ${el.id}: ${el.tagName}`);
        });
        return;
    }
    
    console.log('✅ Found collector-table-body, clearing content...');
    tbody.innerHTML = '';
    
    if (data.length === 0) {
        console.log('📋 No collectors data, showing empty message');
        tbody.innerHTML = `
            <tr>
                <td colspan="5" style="text-align: center; color: #666; padding: 40px;">
                    <div style="font-size: 18px; margin-bottom: 10px;">📋 Không có collector nào được cấu hình</div>
                    <div style="font-size: 14px; color: #999;">Hãy thêm collector đầu tiên bằng nút "Add" ở trên</div>
                </td>
            </tr>
        `;
        return;
    }
    
    console.log('📊 Processing collectors data...');
    
    // Thêm thống kê tổng quan
    const totalCollectors = data.length;
    const runningCollectors = data.filter(item => item.status === 'running').length;
    const stoppedCollectors = totalCollectors - runningCollectors;
    
    // Cập nhật header với thống kê
    const header = document.querySelector('#tab-dashboard .main-header h1');
    if (header) {
        header.innerHTML = `Log Sources <span style="font-size: 16px; color: #666; font-weight: normal;">(${totalCollectors} total, ${runningCollectors} active, ${stoppedCollectors} stopped)</span>`;
        console.log('✅ Header updated with stats');
    } else {
        console.warn('⚠️ Header element not found');
    }
    
    console.log(`🔄 Starting to create ${data.length} rows...`);
    
    data.forEach((item, idx) => {
        const isRunning = item.status === 'running';
        const statusColor = isRunning ? '#10B981' : '#EF4444';
        const statusText = isRunning ? '🟢 Active' : '🔴 Inactive';
        const typeIcon = getTypeIcon(item.type);
        
        console.log(`  📝 Creating row ${idx + 1}/${data.length}: ${item.name} (${item.type}) - ${item.status}`);
        
        const tr = document.createElement('tr');
        tr.style.transition = 'all 0.2s ease';
        tr.innerHTML = `
            <td style="text-align: center;">
                <input type="checkbox" class="row-checkbox" data-idx="${idx}" style="transform: scale(1.2);">
            </td>
            <td style="font-weight: 600; color: #E5E7EB;">
                <div style="display: flex; align-items: center; gap: 8px;">
                    <span style="font-size: 18px;">${typeIcon}</span>
                    <span>${item.name || 'Unknown'}</span>
                </div>
                <div style="font-size: 11px; color: #6B7280; margin-top: 2px;">
                    Tag: <code style="background: #F3F4F6; padding: 1px 4px; border-radius: 2px;">${item.tag}</code>
                </div>
            </td>
            <td style="color: #6B7280; font-family: monospace; font-size: 12px;">
                <span style="background: #F3F4F6; padding: 4px 8px; border-radius: 4px;">${item.type}</span>
            </td>
            <td style="text-align: center;">
                <span style="color: ${statusColor}; font-weight: 600; display: inline-flex; align-items: center; gap: 4px;">
                    ${statusText}
                </span>
                <div style="font-size: 11px; color: #6B7280; margin-top: 2px;">
                    ${isRunning ? 
                        `📁 Log: .\\logs\\${item.tag}.log` : 
                        `❌ Log disabled`
                    }
                </div>
            </td>
            <td style="text-align: center;">
                <div style="display: flex; gap: 4px; justify-content: center;">
                    <button class="btn-toggle" 
                            onclick="toggleCollector('${item.tag}', ${isRunning})"
                            style="
                                padding: 4px 8px;
                                border: none;
                                border-radius: 4px;
                                font-size: 11px;
                                cursor: pointer;
                                transition: all 0.2s;
                                ${isRunning ? 
                                    'background: #EF4444; color: white;' : 
                                    'background: #10B981; color: white;'
                                }
                            ">
                        ${isRunning ? '🔴 Stop' : '🟢 Start'}
                    </button>
                </div>
            </td>
        `;
        
        // Gắn sự kiện cho checkbox
        const checkbox = tr.querySelector('.row-checkbox');
        checkbox.onclick = function(e) {
            e.stopPropagation();
            if (this.checked) {
                selectedCollectorIndex = idx;
                window.selectedCollectorIndex = idx; // Update global
                // Bỏ chọn tất cả checkbox khác
                document.querySelectorAll('.row-checkbox').forEach(cb => {
                    if (cb !== this) cb.checked = false;
                });
                // Highlight row được chọn
                document.querySelectorAll('#collector-table-body tr').forEach(row => {
                    row.classList.remove('selected');
                });
                tr.classList.add('selected');
                console.log(`✅ Selected collector: ${item.name} (index: ${idx})`);
                
                // Cập nhật trạng thái buttons
                if (window.updateButtonStates) {
                    window.updateButtonStates();
                }
            } else {
                selectedCollectorIndex = null;
                window.selectedCollectorIndex = null; // Update global
                tr.classList.remove('selected');
                console.log('❌ Deselected collector');
                
                // Cập nhật trạng thái buttons
                if (window.updateButtonStates) {
                    window.updateButtonStates();
                }
            }
        };
        
        tbody.appendChild(tr);
        console.log(`  ✅ Row ${idx + 1} added to table`);
    });
    
    console.log(`📊 Current table row count: ${tbody.children.length}`);
    
    // Gắn sự kiện cho select all checkbox
    const selectAllCheckbox = document.getElementById('selectAllCheckbox');
    if (selectAllCheckbox) {
        selectAllCheckbox.onchange = function() {
            const checkboxes = document.querySelectorAll('.row-checkbox');
            checkboxes.forEach((checkbox, idx) => {
                checkbox.checked = this.checked;
                if (this.checked) {
                    selectedCollectorIndex = idx;
                    window.selectedCollectorIndex = idx; // Update global
                    checkbox.closest('tr').classList.add('selected');
                } else {
                    selectedCollectorIndex = null;
                    window.selectedCollectorIndex = null; // Update global
                    checkbox.closest('tr').classList.remove('selected');
                }
            });
            // Cập nhật trạng thái buttons
            if (window.updateButtonStates) {
                window.updateButtonStates();
            }
        };
    }
    
    console.log(`✅ Rendered ${data.length} collectors successfully`);
    console.log(`📊 Final table row count: ${tbody.children.length}`);
    console.log(`📊 Expected row count: ${data.length}`);
    console.log(`📊 Table HTML preview:`, tbody.innerHTML.substring(0, 200) + '...');
    
    if (tbody.children.length === data.length) {
        console.log('🎉 SUCCESS: All rows added to table!');
    } else {
        console.warn('⚠️ Row count mismatch!');
    }
}

// Hàm toggle collector
function toggleCollector(tag, currentStatus) {
    const enable = !currentStatus;
    console.log(`🔄 Toggling collector: ${tag}, enable: ${enable}`);
    
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
            action: 'toggle',
            tag: tag,
            enable: enable
        });
        console.log(`📤 Sent toggle message: tag=${tag}, enable=${enable}`);
    } else {
        console.error('❌ WebView2 không khả dụng');
        showNotification('Lỗi: WebView2 không khả dụng', 'error');
    }
}

// Hàm nhận danh sách collector từ C# và render bảng
function updateCollectorsFromCSharp(jsonData) {
    console.log('📊 updateCollectorsFromCSharp called with:', jsonData);
    console.log('📊 JSON Data type:', typeof jsonData);
    console.log('📊 JSON Data length:', jsonData ? jsonData.length : 'null');
    console.log('📊 JSON Data preview:', jsonData ? jsonData.substring(0, 100) + '...' : 'null');
    
    let data = [];
    try {
        data = JSON.parse(jsonData);
        console.log('✅ JSON parsed successfully');
    } catch (e) {
        console.error('❌ Error parsing JSON:', e);
        document.getElementById('collector-content').innerHTML = '<div style="color:red">Lỗi dữ liệu collector!</div>';
        return;
    }
    
    console.log('🔄 Calling renderCollectors function directly...');
    renderCollectors(data);
    console.log('✅ Collectors rendered successfully');
}

// Hàm nhận danh sách parser từ C# và render bảng
function updateParsersFromCSharp(jsonData) {
    let data = [];
    try {
        data = JSON.parse(jsonData);
    } catch (e) {
        document.getElementById('parser-content').innerHTML = '<div style="color:red">Lỗi dữ liệu parser!</div>';
        return;
    }
    debugger
    let html = '<button id="btnAddParser">Thêm mới Parser</button>';
    html += '<table class="parser-table"><thead><tr><th>Name</th><th>Format</th><th>Regex</th><th>Time_Key</th><th>Time_Format</th><th>Time_Keep</th><th>Action</th></tr></thead><tbody>';
    data.forEach((item, idx) => {
        html += `<tr><td>${item.Name||''}</td><td>${item.Format||''}</td><td>${item.Regex||''}</td><td>${item.Time_Key||''}</td><td>${item.Time_Format||''}</td><td>${item.Time_Keep||''}</td><td><button class="btnEditParser" data-idx="${idx}">Sửa</button> <button class="btnDeleteParser" data-idx="${idx}">Xóa</button></td></tr>`;
    });
    html += '</tbody></table>';
    document.getElementById('parser-content').innerHTML = html;
}

// Hàm hiển thị thông báo
function showNotification(message, type = 'info') {
    // Tạo notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 12px 20px;
        border-radius: 6px;
        color: white;
        font-weight: 500;
        z-index: 10000;
        max-width: 400px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        transform: translateX(100%);
        transition: transform 0.3s ease;
    `;
    
    // Màu sắc theo loại
    const colors = {
        success: '#10B981',
        error: '#EF4444',
        warning: '#F59E0B',
        info: '#3B82F6'
    };
    notification.style.background = colors[type] || colors.info;
    
    // Icon theo loại
    const icons = {
        success: '✅',
        error: '❌',
        warning: '⚠️',
        info: 'ℹ️'
    };
    notification.innerHTML = `${icons[type] || icons.info} ${message}`;
    
    // Thêm vào body
    document.body.appendChild(notification);
    
    // Hiển thị với animation
    setTimeout(() => {
        notification.style.transform = 'translateX(0)';
    }, 100);
    
    // Tự động ẩn sau 5 giây
    setTimeout(() => {
        notification.style.transform = 'translateX(100%)';
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, 5000);
    
    // Cho phép click để đóng
    notification.onclick = function() {
        this.style.transform = 'translateX(100%)';
        setTimeout(() => {
            if (this.parentNode) {
                this.parentNode.removeChild(this);
            }
        }, 300);
    };
    
    console.log(`📢 Notification [${type}]: ${message}`);
}

// Thêm CSS cho notification vào head
if (!document.getElementById('notification-styles')) {
    const style = document.createElement('style');
    style.id = 'notification-styles';
    style.textContent = `
        .notification {
            cursor: pointer;
            user-select: none;
        }
        .notification:hover {
            opacity: 0.9;
        }
    `;
    document.head.appendChild(style);
}

// Hàm helper để lấy icon theo loại collector
function getTypeIcon(type) {
    const icons = {
        'winlog': '🪟',
        'syslog': '📋',
        'http': '🌐',
        'tail': '📄',
        'dummy': '🎲',
        'tcp': '🔌',
        'udp': '📡',
        'exec': '⚡',
        'forward': '➡️',
        'random': '🎯'
    };
    return icons[type] || '📊';
}

// Các chức năng add, edit, delete đã được xử lý trong collector-actions.js
// Không cần duplicate functions ở đây nữa

// Expose functions globally để C# có thể gọi
window.renderCollectorsFromDashboardModule = renderCollectors;
window.updateCollectorsFromCSharpFromDashboardModule = updateCollectorsFromCSharp;
window.showNotificationFromDashboardModule = showNotification;

console.log('✅ Dashboard.js loaded successfully');
