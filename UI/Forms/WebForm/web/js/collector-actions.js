// Chức năng Add, Edit, Delete cho Collectors
// File này sẽ được load sau dashboard.js
// Sử dụng biến collectors và selectedCollectorIndex từ dashboard.js

// Hàm gửi message tới C#
function sendMessageToCSharp(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
        const message = { action, ...data };
        console.log(`📤 Sending message to C#:`, message);
        window.chrome.webview.postMessage(message);
    } else {
        console.error('❌ WebView2 không khả dụng');
        showNotification('Lỗi: WebView2 không khả dụng', 'error');
    }
}

// Hàm hiển thị popup thêm collector mới
function showAddCollectorPopup() {
    console.log('🔄 Creating add collector popup');
    
    // Tạo form popup
    const popup = document.createElement('div');
    popup.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background: rgba(0,0,0,0.4);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
    `;

    const form = document.createElement('form');
    form.style.cssText = `
        background: white;
        padding: 24px 32px;
        border-radius: 10px;
        min-width: 500px;
        max-width: 80vw;
        max-height: 80vh;
        color: #333;
        box-shadow: 0 10px 25px rgba(0,0,0,0.2);
        overflow-y: auto;
    `;
    form.onsubmit = function(e) { e.preventDefault(); };

    // Header
    const header = document.createElement('h3');
    header.textContent = '➕ Thêm Log Source Mới';
    header.style.cssText = 'margin: 0 0 20px 0; color: #1F2937; font-size: 18px;';
    form.appendChild(header);

    // Chọn loại input
    const labelType = document.createElement('label');
    labelType.textContent = 'Loại log source:';
    labelType.style.cssText = 'display: block; margin-bottom: 8px; font-weight: 600; color: #374151;';
    
    const selectType = document.createElement('select');
    selectType.style.cssText = `
        width: 100%; 
        margin-bottom: 16px; 
        padding: 8px 12px;
        border: 1px solid #D1D5DB;
        border-radius: 6px;
        font-size: 14px;
    `;
    
    // Thêm các loại collector phổ biến
    const collectorTypes = [
        'winlog', 'syslog', 'http', 'tail', 'dummy', 
        'tcp', 'udp', 'exec', 'forward', 'random'
    ];
    
    collectorTypes.forEach(type => {
        const opt = document.createElement('option');
        opt.value = type;
        opt.textContent = type.charAt(0).toUpperCase() + type.slice(1);
        selectType.appendChild(opt);
    });
    
    form.appendChild(labelType);
    form.appendChild(selectType);

    // Container cho các trường động
    const fieldsDiv = document.createElement('div');
    form.appendChild(fieldsDiv);

    // Hàm render các trường theo loại collector
    function renderFields(type) {
        fieldsDiv.innerHTML = '';
        
        // Định nghĩa các trường cho từng loại
        const fieldConfigs = {
            winlog: [
                { name: 'Tag', label: 'Tag:', placeholder: 'winlog', required: true },
                { name: 'Channels', label: 'Channels:', placeholder: 'System,Application', required: true },
                { name: 'Interval_Sec', label: 'Interval (giây):', placeholder: '1', required: false },
                { name: 'DB', label: 'Database:', placeholder: '.\\winlog.db', required: false }
            ],
            syslog: [
                { name: 'Tag', label: 'Tag:', placeholder: 'syslog', required: true },
                { name: 'Listen', label: 'Listen IP:', placeholder: '0.0.0.0', required: true },
                { name: 'Port', label: 'Port:', placeholder: '514', required: true },
                { name: 'Mode', label: 'Mode:', placeholder: 'tcp', required: true },
                { name: 'Parser', label: 'Parser:', placeholder: 'syslog-rfc5424', required: false }
            ],
            http: [
                { name: 'Tag', label: 'Tag:', placeholder: 'http', required: true },
                { name: 'Host', label: 'Host:', placeholder: '0.0.0.0', required: true },
                { name: 'Port', label: 'Port:', placeholder: '8888', required: true }
            ],
            tail: [
                { name: 'Path', label: 'Đường dẫn file:', placeholder: 'C:\\logs\\*.log', required: true },
                { name: 'Tag', label: 'Tag:', placeholder: 'tail', required: true }
            ],
            dummy: [
                { name: 'Tag', label: 'Tag:', placeholder: 'dummy', required: true },
                { name: 'Dummy', label: 'Dummy data:', placeholder: '{"message": "test"}', required: true },
                { name: 'Samples', label: 'Samples:', placeholder: '1000', required: false }
            ]
        };

        const fields = fieldConfigs[type] || [
            { name: 'Tag', label: 'Tag:', placeholder: 'tag_name', required: true }
        ];

        fields.forEach(field => {
            const label = document.createElement('label');
            label.textContent = field.label;
            label.style.cssText = 'display: block; margin-top: 12px; font-weight: 600; color: #374151;';
            
            const input = document.createElement('input');
            input.type = 'text';
            input.name = field.name;
            input.placeholder = field.placeholder;
            input.required = field.required;
            input.style.cssText = `
                width: 100%; 
                margin-bottom: 8px; 
                padding: 8px 12px;
                border: 1px solid #D1D5DB;
                border-radius: 6px;
                font-size: 14px;
                box-sizing: border-box;
            `;
            
            fieldsDiv.appendChild(label);
            fieldsDiv.appendChild(input);
        });
    }

    // Khi chọn loại input, render các trường tương ứng
    selectType.onchange = function() {
        renderFields(this.value);
    };
    
    // Lần đầu render
    renderFields(selectType.value);

    // Phần Output Configuration
    const outputSection = document.createElement('div');
    outputSection.style.cssText = 'margin-top: 24px; border-top: 1px solid #E5E7EB; padding-top: 20px;';
    
    const outputHeader = document.createElement('h4');
    outputHeader.textContent = '📤 Cấu hình Output';
    outputHeader.style.cssText = 'margin: 0 0 16px 0; color: #1F2937; font-size: 16px;';
    outputSection.appendChild(outputHeader);
    
    const outputDescription = document.createElement('p');
    outputDescription.textContent = 'Chọn cách xử lý log sau khi thu thập:';
    outputDescription.style.cssText = 'margin: 0 0 16px 0; color: #6B7280; font-size: 14px;';
    outputSection.appendChild(outputDescription);

    // Output type selection
    const outputTypeDiv = document.createElement('div');
    outputTypeDiv.style.cssText = 'margin-bottom: 16px;';
    
    const outputTypeLabel = document.createElement('label');
    outputTypeLabel.textContent = 'Loại Output:';
    outputTypeLabel.style.cssText = 'display: block; margin-bottom: 8px; font-weight: 600; color: #374151;';
    
    const outputTypeSelect = document.createElement('select');
    outputTypeSelect.style.cssText = `
        width: 100%; 
        margin-bottom: 16px; 
        padding: 8px 12px;
        border: 1px solid #D1D5DB;
        border-radius: 6px;
        font-size: 14px;
    `;
    
    const outputTypes = [
        { value: 'file', label: '📄 File - Ghi log ra file', default: true },
        { value: 'opensearch', label: '🔍 OpenSearch - Gửi lên OpenSearch' },
        { value: 'http', label: '🌐 HTTP - Gửi qua HTTP API' },
        { value: 'forward', label: '➡️ Forward - Chuyển tiếp tới Fluentd' }
    ];
    
    outputTypes.forEach(outputType => {
        const opt = document.createElement('option');
        opt.value = outputType.value;
        opt.textContent = outputType.label;
        if (outputType.default) opt.selected = true;
        outputTypeSelect.appendChild(opt);
    });
    
    outputTypeDiv.appendChild(outputTypeLabel);
    outputTypeDiv.appendChild(outputTypeSelect);
    outputSection.appendChild(outputTypeDiv);

    // Output fields container
    const outputFieldsDiv = document.createElement('div');
    outputSection.appendChild(outputFieldsDiv);

    // Hàm render output fields theo loại
    function renderOutputFields(outputType) {
        outputFieldsDiv.innerHTML = '';
        
        const outputFieldConfigs = {
            file: [
                { name: 'Path', label: 'Đường dẫn thư mục:', value: '.\\logs\\', required: true },
                { name: 'File', label: 'Tên file:', value: '', required: true, placeholder: 'auto-generated' },
                { name: 'Format', label: 'Định dạng:', value: 'plain', required: false },
                { name: 'Retry_Limit', label: 'Số lần thử lại:', value: '3', required: false }
            ],
            opensearch: [
                { name: 'Host', label: 'OpenSearch Host:', value: 'localhost', required: true },
                { name: 'Port', label: 'Port:', value: '9200', required: true },
                { name: 'Index', label: 'Index name:', value: 'logs', required: true },
                { name: 'HTTP_User', label: 'Username:', value: '', required: false },
                { name: 'HTTP_Passwd', label: 'Password:', value: '', required: false, type: 'password' }
            ],
            http: [
                { name: 'Host', label: 'HTTP Host:', value: 'localhost', required: true },
                { name: 'Port', label: 'Port:', value: '8080', required: true },
                { name: 'URI', label: 'URI path:', value: '/logs', required: true },
                { name: 'HTTP_User', label: 'Username:', value: '', required: false },
                { name: 'HTTP_Passwd', label: 'Password:', value: '', required: false, type: 'password' }
            ],
            forward: [
                { name: 'Host', label: 'Fluentd Host:', value: 'localhost', required: true },
                { name: 'Port', label: 'Port:', value: '24224', required: true },
                { name: 'Shared_Key', label: 'Shared Key:', value: '', required: false }
            ]
        };

        const fields = outputFieldConfigs[outputType] || [];
        
        fields.forEach(field => {
            const label = document.createElement('label');
            label.textContent = field.label;
            label.style.cssText = 'display: block; margin-top: 12px; font-weight: 600; color: #374151;';
            
            const input = document.createElement('input');
            input.type = field.type || 'text';
            input.name = field.name;
            input.value = field.value;
            input.placeholder = field.placeholder || '';
            input.required = field.required;
            input.style.cssText = `
                width: 100%; 
                margin-bottom: 8px; 
                padding: 8px 12px;
                border: 1px solid #D1D5DB;
                border-radius: 6px;
                font-size: 14px;
                box-sizing: border-box;
            `;
            
            outputFieldsDiv.appendChild(label);
            outputFieldsDiv.appendChild(input);
        });
    }

    // Khi chọn loại output, render các trường tương ứng
    outputTypeSelect.onchange = function() {
        renderOutputFields(this.value);
    };
    
    // Lần đầu render output fields
    renderOutputFields(outputTypeSelect.value);

    form.appendChild(outputSection);

    // Nút OK và Cancel
    const buttonDiv = document.createElement('div');
    buttonDiv.style.cssText = 'display: flex; gap: 12px; margin-top: 24px; justify-content: flex-end;';
    
    const btnCancel = document.createElement('button');
    btnCancel.type = 'button';
    btnCancel.textContent = '❌ Hủy';
    btnCancel.style.cssText = `
        padding: 8px 16px;
        border: 1px solid #D1D5DB;
        border-radius: 6px;
        background: white;
        color: #374151;
        cursor: pointer;
        font-size: 14px;
    `;
    btnCancel.onclick = function() {
        document.body.removeChild(popup);
    };
    
    const btnOk = document.createElement('button');
    btnOk.type = 'button';
    btnOk.textContent = '✅ Thêm';
    btnOk.style.cssText = `
        padding: 8px 16px;
        border: none;
        border-radius: 6px;
        background: #10B981;
        color: white;
        cursor: pointer;
        font-size: 14px;
        font-weight: 600;
    `;
    btnOk.onclick = function() {
        const type = selectType.value;
        const outputType = outputTypeSelect.value;
        const data = { type };
        let valid = true;
        
        // Lấy dữ liệu từ form input
        const inputs = form.querySelectorAll('input');
        inputs.forEach(input => {
            const value = input.value.trim();
            if (input.required && !value) {
                valid = false;
                input.style.borderColor = '#EF4444';
            } else {
                input.style.borderColor = '#D1D5DB';
            }
            data[input.name] = value;
        });
        
        if (!valid) {
            showNotification('Vui lòng nhập đầy đủ các trường bắt buộc!', 'warning');
            return;
        }
        
        // Tạo output configuration
        const output = {
            type: outputType
        };
        
        // Lấy dữ liệu từ output fields
        const outputInputs = outputFieldsDiv.querySelectorAll('input');
        outputInputs.forEach(input => {
            const value = input.value.trim();
            if (value) {
                output[input.name] = value;
            }
        });
        
        // Nếu là file output và không có tên file, tự động tạo
        if (outputType === 'file' && !output.File) {
            output.File = `${data.Tag}.log`;
        }
        
        data.outputs = [output];
        
        console.log('🔄 Adding collector with output:', data);
        sendMessageToCSharp('add_collector_with_output', { data });
        showNotification('Đang thêm collector mới với output...', 'info');
        document.body.removeChild(popup);
    };
    
    buttonDiv.appendChild(btnCancel);
    buttonDiv.appendChild(btnOk);
    form.appendChild(buttonDiv);
    popup.appendChild(form);
    document.body.appendChild(popup);
    
    console.log('✅ Add collector popup with output created');
}

// Hàm hiển thị popup sửa collector
function showEditCollectorPopup(collector, idx) {
    console.log('🔄 Creating edit collector popup for:', collector, 'index:', idx);
    
    const popup = document.createElement('div');
    popup.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background: rgba(0,0,0,0.4);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
    `;

    const form = document.createElement('form');
    form.style.cssText = `
        background: white;
        padding: 24px 32px;
        border-radius: 10px;
        min-width: 400px;
        color: #333;
        box-shadow: 0 10px 25px rgba(0,0,0,0.2);
    `;
    form.onsubmit = function(e) { e.preventDefault(); };

    // Header
    const header = document.createElement('h3');
    header.textContent = '✏️ Sửa Log Source';
    header.style.cssText = 'margin: 0 0 20px 0; color: #1F2937; font-size: 18px;';
    form.appendChild(header);

    // Hiển thị thông tin collector hiện tại
    const infoDiv = document.createElement('div');
    infoDiv.style.cssText = `
        background: #F3F4F6;
        padding: 12px;
        border-radius: 6px;
        margin-bottom: 16px;
        font-size: 14px;
    `;
    infoDiv.innerHTML = `
        <strong>Loại:</strong> ${collector.type}<br>
        <strong>Tag:</strong> ${collector.tag}<br>
        <strong>Trạng thái:</strong> ${collector.status === 'running' ? '🟢 Active' : '🔴 Inactive'}
    `;
    form.appendChild(infoDiv);

    // Các trường để sửa
    const fieldsDiv = document.createElement('div');
    form.appendChild(fieldsDiv);

    function renderFields(type) {
        fieldsDiv.innerHTML = '';
        
        const fieldConfigs = {
            winlog: [
                { name: 'Channels', label: 'Channels:', value: 'System,Application', required: true },
                { name: 'Interval_Sec', label: 'Interval (giây):', value: '1', required: false },
                { name: 'DB', label: 'Database:', value: '.\\winlog.db', required: false }
            ],
            syslog: [
                { name: 'Listen', label: 'Listen IP:', value: '0.0.0.0', required: true },
                { name: 'Port', label: 'Port:', value: '514', required: true },
                { name: 'Mode', label: 'Mode:', value: 'tcp', required: true },
                { name: 'Parser', label: 'Parser:', value: 'syslog-rfc5424', required: false }
            ]
        };

        const fields = fieldConfigs[type] || [
            { name: 'Tag', label: 'Tag:', value: collector.tag || '', required: true }
        ];

        fields.forEach(field => {
            const label = document.createElement('label');
            label.textContent = field.label;
            label.style.cssText = 'display: block; margin-top: 12px; font-weight: 600; color: #374151;';
            
            const input = document.createElement('input');
            input.type = 'text';
            input.name = field.name;
            input.value = field.value;
            input.required = field.required;
            input.style.cssText = `
                width: 100%; 
                margin-bottom: 8px; 
                padding: 8px 12px;
                border: 1px solid #D1D5DB;
                border-radius: 6px;
                font-size: 14px;
                box-sizing: border-box;
            `;
            
            fieldsDiv.appendChild(label);
            fieldsDiv.appendChild(input);
        });
    }

    renderFields(collector.type);

    // Nút OK và Cancel
    const buttonDiv = document.createElement('div');
    buttonDiv.style.cssText = 'display: flex; gap: 12px; margin-top: 24px; justify-content: flex-end;';
    
    const btnCancel = document.createElement('button');
    btnCancel.type = 'button';
    btnCancel.textContent = '❌ Hủy';
    btnCancel.style.cssText = `
        padding: 8px 16px;
        border: 1px solid #D1D5DB;
        border-radius: 6px;
        background: white;
        color: #374151;
        cursor: pointer;
        font-size: 14px;
    `;
    btnCancel.onclick = function() {
        document.body.removeChild(popup);
    };
    
    const btnOk = document.createElement('button');
    btnOk.type = 'button';
    btnOk.textContent = '✅ Lưu thay đổi';
    btnOk.style.cssText = `
        padding: 8px 16px;
        border: none;
        border-radius: 6px;
        background: #3B82F6;
        color: white;
        cursor: pointer;
        font-size: 14px;
        font-weight: 600;
    `;
    btnOk.onclick = function() {
        const data = { type: collector.type, Tag: collector.tag };
        let valid = true;
        
        const inputs = form.querySelectorAll('input');
        inputs.forEach(input => {
            const value = input.value.trim();
            if (input.required && !value) {
                valid = false;
                input.style.borderColor = '#EF4444';
            } else {
                input.style.borderColor = '#D1D5DB';
            }
            data[input.name] = value;
        });
        
        if (!valid) {
            showNotification('Vui lòng nhập đầy đủ các trường bắt buộc!', 'warning');
            return;
        }
        
        console.log('🔄 Editing collector with data:', data);
        sendMessageToCSharp('edit_collector', { data, idx });
        showNotification('Đang cập nhật collector...', 'info');
        document.body.removeChild(popup);
    };
    
    buttonDiv.appendChild(btnCancel);
    buttonDiv.appendChild(btnOk);
    form.appendChild(buttonDiv);
    popup.appendChild(form);
    document.body.appendChild(popup);
    
    console.log('✅ Edit collector popup created');
}

// Hàm xóa collector
function deleteCollector(idx) {
    if (idx === null || idx === undefined) {
        showNotification('Vui lòng chọn một log source để xóa!', 'warning');
        return;
    }
    
    const collector = collectors[idx];
    if (!collector) {
        showNotification('Không tìm thấy thông tin collector!', 'error');
        return;
    }
    
    const collectorName = collector.name || collector.tag;
    
    if (confirm(`Bạn có chắc muốn xóa log source "${collectorName}"?\n\n⚠️ Hành động này sẽ:\n❌ Xóa hoàn toàn collector khỏi cấu hình\n❌ Dừng việc thu thập log\n❌ Không thể hoàn tác`)) {
        console.log(`🔄 Deleting collector: ${collectorName} (index: ${idx})`);
        sendMessageToCSharp('delete_collector', { idx });
        showNotification(`Đang xóa collector "${collectorName}"...`, 'info');
    }
}

// Khởi tạo các sự kiện khi DOM ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('🔧 Initializing collector action buttons...');
    
    // Nút Add
    const btnAdd = document.getElementById('btnAdd');
    if (btnAdd) {
        btnAdd.onclick = function() {
            console.log('🔄 Add button clicked');
            showAddCollectorPopup();
        };
        console.log('✅ Add button initialized');
    } else {
        console.warn('⚠️ Add button not found');
    }
    
    // Nút Edit
    const btnEdit = document.getElementById('btnEdit');
    if (btnEdit) {
        btnEdit.onclick = function() {
            console.log('🔄 Edit button clicked');
            if (window.selectedCollectorIndex === null) {
                showNotification('Vui lòng chọn một log source để sửa!', 'warning');
                return;
            }
            const collector = window.collectors[window.selectedCollectorIndex];
            if (collector) {
                showEditCollectorPopup(collector, window.selectedCollectorIndex);
            } else {
                showNotification('Không tìm thấy thông tin collector!', 'error');
            }
        };
        console.log('✅ Edit button initialized');
    } else {
        console.warn('⚠️ Edit button not found');
    }
    
    // Nút Delete
    const btnDelete = document.getElementById('btnDelete');
    if (btnDelete) {
        btnDelete.onclick = function() {
            console.log('🔄 Delete button clicked');
            deleteCollector(window.selectedCollectorIndex);
        };
        console.log('✅ Delete button initialized');
    } else {
        console.warn('⚠️ Delete button not found');
    }
    
    // Nút Show Events
    const btnShowEvents = document.getElementById('btnShowEvents');
    if (btnShowEvents) {
        btnShowEvents.onclick = function() {
            console.log('🔄 Show Events button clicked');
            if (window.selectedCollectorIndex === null) {
                showNotification('Vui lòng chọn một log source để xem sự kiện!', 'warning');
                return;
            }
            sendMessageToCSharp('show_events', { idx: window.selectedCollectorIndex });
        };
        console.log('✅ Show Events button initialized');
    } else {
        console.warn('⚠️ Show Events button not found');
    }
    
    // Nút Enable Log
    const btnEnableLog = document.getElementById('btnEnableLog');
    if (btnEnableLog) {
        btnEnableLog.onclick = function() {
            console.log('🔄 Enable Log button clicked');
            if (window.selectedCollectorIndex === null) {
                showNotification('Vui lòng chọn một log source để bật ghi log!', 'warning');
                return;
            }
            const collector = window.collectors[window.selectedCollectorIndex];
            if (collector) {
                const tag = collector.tag;
                sendMessageToCSharp('toggle', { tag, enable: true });
            } else {
                showNotification('Không tìm thấy thông tin collector!', 'error');
            }
        };
        console.log('✅ Enable Log button initialized');
    } else {
        console.warn('⚠️ Enable Log button not found');
    }
    
    // Nút Disable Log
    const btnDisableLog = document.getElementById('btnDisableLog');
    if (btnDisableLog) {
        btnDisableLog.onclick = function() {
            console.log('🔄 Disable Log button clicked');
            if (window.selectedCollectorIndex === null) {
                showNotification('Vui lòng chọn một log source để tắt ghi log!', 'warning');
                return;
            }
            const collector = window.collectors[window.selectedCollectorIndex];
            if (collector) {
                const tag = collector.tag;
                sendMessageToCSharp('toggle', { tag, enable: false });
            } else {
                showNotification('Không tìm thấy thông tin collector!', 'error');
            }
        };
        console.log('✅ Disable Log button initialized');
    } else {
        console.warn('⚠️ Disable Log button not found');
    }
    
    // Search functionality
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.oninput = function() {
            const searchTerm = this.value.toLowerCase().trim();
            console.log('🔍 Searching for:', searchTerm);
            
            if (searchTerm === '') {
                // Hiển thị tất cả collectors
                if (window.collectors && window.collectors.length > 0) {
                    // Trigger re-render nếu cần
                    console.log('🔍 Search cleared, showing all collectors');
                }
            } else {
                // Filter collectors
                const filteredCollectors = window.collectors ? window.collectors.filter(collector => 
                    collector.name && collector.name.toLowerCase().includes(searchTerm) ||
                    collector.tag && collector.tag.toLowerCase().includes(searchTerm) ||
                    collector.type && collector.type.toLowerCase().includes(searchTerm)
                ) : [];
                console.log('🔍 Filtered collectors:', filteredCollectors);
                
                // Có thể implement highlight search results ở đây
            }
        };
        console.log('✅ Search input initialized');
    } else {
        console.warn('⚠️ Search input not found');
    }
    
    // Hàm cập nhật trạng thái buttons
    function updateButtonStates() {
        const hasSelection = window.selectedCollectorIndex !== null;
        const buttons = [btnEdit, btnDelete, btnShowEvents, btnEnableLog, btnDisableLog];
        
        buttons.forEach(btn => {
            if (btn) {
                btn.disabled = !hasSelection;
                btn.style.opacity = hasSelection ? '1' : '0.5';
                btn.style.cursor = hasSelection ? 'pointer' : 'not-allowed';
            }
        });
        
        console.log('🔧 Button states updated:', hasSelection ? 'enabled' : 'disabled');
    }
    
    // Cập nhật trạng thái buttons ban đầu
    updateButtonStates();
    
    // Expose function để dashboard có thể gọi
    window.updateButtonStates = updateButtonStates;
    
    console.log('✅ All collector action buttons initialized');
});

// Expose functions globally để C# có thể gọi
window.showAddCollectorPopupFromActionsModule = showAddCollectorPopup;
window.showEditCollectorPopupFromActionsModule = showEditCollectorPopup;
window.deleteCollectorFromActionsModule = deleteCollector;
window.sendMessageToCSharpFromActionsModule = sendMessageToCSharp;

console.log('✅ Collector actions module loaded successfully');
