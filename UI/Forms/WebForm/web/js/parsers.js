// Quản lý parsers cho dashboard
// Sử dụng global variables thay vì ES6 modules để tương thích với script loading

// Global variables - sẽ được set sau khi các script khác load
let PopupManager = window.PopupManager || class {};

// Expose functions globally
window.ParserManager = class ParserManager {
    constructor() {
        this.parsers = [];
        this.initEventListeners();
    }
    
    initEventListeners() {
        // Parser tab click event
        const parserTab = document.querySelector('.sidebar-item[data-tab="parser"]');
        if (parserTab) {
            parserTab.addEventListener('click', () => {
                this.loadParsers();
            });
        }
    }
    
    loadParsers() {
        sendMessageToCSharp('get_parsers');
    }
    
    renderParsers(jsonData) {
        let data = [];
        try {
            data = JSON.parse(jsonData);
        } catch (e) {
            document.getElementById('parser-content').innerHTML = 
                '<div style="color:red">Lỗi dữ liệu parser!</div>';
            return;
        }
        
        this.parsers = data;
        this.renderParserTable();
    }
    
    renderParserTable() {
        const container = document.getElementById('parser-content');
        
        let html = `
            <div class="parser-header" style="margin-bottom: 24px;">
                <h2 style="color: #F9FAFB; margin: 0 0 8px 0; font-size: 24px;">🔧 Parser Configuration</h2>
                <p style="color: #D1D5DB; margin: 0; font-size: 14px;">Quản lý các parser để parse log data từ các input sources</p>
            </div>
            
            <div class="parser-controls" style="margin-bottom: 20px;">
                <button id="btnAddParser" class="btn-primary" style="
                    padding: 10px 20px;
                    background: #10B981;
                    color: white;
                    border: none;
                    border-radius: 6px;
                    cursor: pointer;
                    font-size: 14px;
                    font-weight: 600;
                ">➕ Thêm mới Parser</button>
            </div>
            
            <div class="parser-table-container" style="background: #1F2937; border-radius: 8px; overflow: hidden; border: 1px solid #374151;">
                <table class="parser-table" style="width: 100%; border-collapse: collapse;">
                    <thead>
                        <tr style="background: #374151;">
                            <th style="padding: 12px; text-align: left; color: #F9FAFB; font-weight: 600; border-bottom: 1px solid #4B5563;">Name</th>
                            <th style="padding: 12px; text-align: left; color: #F9FAFB; font-weight: 600; border-bottom: 1px solid #4B5563;">Format</th>
                            <th style="padding: 12px; text-align: left; color: #F9FAFB; font-weight: 600; border-bottom: 1px solid #4B5563;">Regex</th>
                            <th style="padding: 12px; text-align: left; color: #F9FAFB; font-weight: 600; border-bottom: 1px solid #4B5563;">Time_Key</th>
                            <th style="padding: 12px; text-align: left; color: #F9FAFB; font-weight: 600; border-bottom: 1px solid #4B5563;">Time_Format</th>
                            <th style="padding: 12px; text-align: left; color: #F9FAFB; font-weight: 600; border-bottom: 1px solid #4B5563;">Time_Keep</th>
                            <th style="padding: 12px; text-align: center; color: #F9FAFB; font-weight: 600; border-bottom: 1px solid #4B5563;">Action</th>
                        </tr>
                    </thead>
                    <tbody>
        `;
        
        if (this.parsers.length === 0) {
            html += `
                <tr>
                    <td colspan="7" style="padding: 40px; text-align: center; color: #9CA3AF;">
                        <div style="font-size: 16px; margin-bottom: 8px;">📋 Không có parser nào được cấu hình</div>
                        <div style="font-size: 14px;">Hãy thêm parser đầu tiên để parse log data</div>
                    </td>
                </tr>
            `;
        } else {
            this.parsers.forEach((item, idx) => {
                html += `
                    <tr style="border-bottom: 1px solid #374151;">
                        <td style="padding: 12px; color: #F9FAFB; font-weight: 600;">${item.Name || ''}</td>
                        <td style="padding: 12px; color: #D1D5DB;">
                            <span style="background: #374151; padding: 4px 8px; border-radius: 4px; font-family: monospace; font-size: 12px;">${item.Format || ''}</span>
                        </td>
                        <td style="padding: 12px; color: #D1D5DB;">
                            <div style="max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-family: monospace; font-size: 11px;" title="${item.Regex || ''}">${item.Regex || ''}</div>
                        </td>
                        <td style="padding: 12px; color: #D1D5DB;">${item.Time_Key || ''}</td>
                        <td style="padding: 12px; color: #D1D5DB;">
                            <span style="background: #374151; padding: 4px 8px; border-radius: 4px; font-family: monospace; font-size: 11px;">${item.Time_Format || ''}</span>
                        </td>
                        <td style="padding: 12px; color: #D1D5DB;">${item.Time_Keep || ''}</td>
                        <td style="padding: 12px; text-align: center;">
                            <button class="btnEditParser" data-idx="${idx}" style="
                                padding: 6px 12px;
                                background: #3B82F6;
                                color: white;
                                border: none;
                                border-radius: 4px;
                                cursor: pointer;
                                font-size: 12px;
                                margin-right: 4px;
                            ">✏️ Sửa</button>
                            <button class="btnDeleteParser" data-idx="${idx}" style="
                                padding: 6px 12px;
                                background: #EF4444;
                                color: white;
                                border: none;
                                border-radius: 4px;
                                cursor: pointer;
                                font-size: 12px;
                            ">🗑️ Xóa</button>
                        </td>
                    </tr>
                `;
            });
        }
        
        html += '</tbody></table></div>';
        container.innerHTML = html;
        
        this.attachParserEventListeners();
    }
    
    attachParserEventListeners() {
        // Add parser button
        const addBtn = document.getElementById('btnAddParser');
        if (addBtn) {
            addBtn.onclick = () => this.showAddParserPopup();
        }
        
        // Edit parser buttons
        document.querySelectorAll('.btnEditParser').forEach(btn => {
            btn.onclick = (e) => {
                const idx = parseInt(e.target.getAttribute('data-idx'));
                this.showEditParserPopup(idx);
            };
        });
        
        // Delete parser buttons
        document.querySelectorAll('.btnDeleteParser').forEach(btn => {
            btn.onclick = (e) => {
                const idx = parseInt(e.target.getAttribute('data-idx'));
                this.deleteParser(idx);
            };
        });
    }
    
    showAddParserPopup() {
        this.showParserPopup();
    }
    
    showEditParserPopup(idx) {
        const parser = this.parsers[idx];
        if (parser) {
            this.showParserPopup(parser, idx);
        }
    }
    
    showParserPopup(parser = null, editIdx = null) {
        const isEdit = parser !== null;
        const title = isEdit ? '✏️ Sửa Parser' : '➕ Thêm mới Parser';
        
        // Tạo popup
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
            background: #1F2937;
            padding: 24px 32px;
            border-radius: 10px;
            min-width: 500px;
            max-width: 80vw;
            max-height: 80vh;
            color: #F9FAFB;
            box-shadow: 0 10px 25px rgba(0,0,0,0.2);
            overflow-y: auto;
            border: 1px solid #374151;
        `;
        form.onsubmit = function(e) { e.preventDefault(); };

        // Header
        const header = document.createElement('h3');
        header.textContent = title;
        header.style.cssText = 'margin: 0 0 20px 0; color: #F9FAFB; font-size: 18px;';
        form.appendChild(header);

        // Parser fields
        const fields = [
            { name: 'Name', label: 'Tên Parser:', placeholder: 'my_parser', required: true, value: parser?.Name || '' },
            { name: 'Format', label: 'Format:', placeholder: 'regex', required: true, value: parser?.Format || 'regex', options: ['regex', 'json', 'csv', 'ltsv', 'logfmt', 'none'] },
            { name: 'Regex', label: 'Regular Expression:', placeholder: '^(?<time>\\d{4}-\\d{2}-\\d{2}) (?<level>\\w+) (?<message>.*)$', required: false, value: parser?.Regex || '', type: 'textarea' },
            { name: 'Time_Key', label: 'Time Key:', placeholder: 'time', required: false, value: parser?.Time_Key || '' },
            { name: 'Time_Format', label: 'Time Format:', placeholder: '%Y-%m-%d %H:%M:%S', required: false, value: parser?.Time_Format || '' },
            { name: 'Time_Keep', label: 'Time Keep:', placeholder: 'On', required: false, value: parser?.Time_Keep || 'On', options: ['On', 'Off'] }
        ];

        fields.forEach(field => {
            const label = document.createElement('label');
            label.textContent = field.label;
            label.style.cssText = 'display: block; margin-top: 16px; font-weight: 600; color: #F9FAFB;';
            
            let input;
            if (field.options) {
                input = document.createElement('select');
                field.options.forEach(option => {
                    const opt = document.createElement('option');
                    opt.value = option;
                    opt.textContent = option;
                    if (option === field.value) opt.selected = true;
                    input.appendChild(opt);
                });
            } else if (field.type === 'textarea') {
                input = document.createElement('textarea');
                input.rows = 4;
                input.placeholder = field.placeholder;
                input.value = field.value;
            } else {
                input = document.createElement('input');
                input.type = 'text';
                input.placeholder = field.placeholder;
                input.value = field.value;
            }
            
            input.required = field.required;
            input.style.cssText = `
                width: 100%; 
                margin-bottom: 8px; 
                padding: 8px 12px;
                border: 1px solid #4B5563;
                border-radius: 6px;
                font-size: 14px;
                box-sizing: border-box;
                background: #374151;
                color: #F9FAFB;
            `;
            
            form.appendChild(label);
            form.appendChild(input);
        });

        // Buttons
        const buttonDiv = document.createElement('div');
        buttonDiv.style.cssText = 'display: flex; gap: 12px; margin-top: 24px; justify-content: flex-end;';
        
        const btnCancel = document.createElement('button');
        btnCancel.type = 'button';
        btnCancel.textContent = '❌ Hủy';
        btnCancel.style.cssText = `
            padding: 8px 16px;
            border: 1px solid #4B5563;
            border-radius: 6px;
            background: #374151;
            color: #F9FAFB;
            cursor: pointer;
            font-size: 14px;
        `;
        btnCancel.onclick = function() {
            document.body.removeChild(popup);
        };
        
        const btnOk = document.createElement('button');
        btnOk.type = 'button';
        btnOk.textContent = isEdit ? '✅ Lưu thay đổi' : '✅ Thêm';
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
        btnOk.onclick = () => {
            this.saveParser(form, editIdx);
            document.body.removeChild(popup);
        };
        
        buttonDiv.appendChild(btnCancel);
        buttonDiv.appendChild(btnOk);
        form.appendChild(buttonDiv);
        popup.appendChild(form);
        document.body.appendChild(popup);
    }
    
    saveParser(form, editIdx) {
        const data = {};
        const inputs = form.querySelectorAll('input, select, textarea');
        
        inputs.forEach(input => {
            if (input.name) {
                data[input.name] = input.value.trim();
            }
        });
        
        // Validation
        if (!data.Name || !data.Format) {
            showNotification('Vui lòng nhập đầy đủ các trường bắt buộc!', 'warning');
            return;
        }
        
        if (editIdx !== null) {
            // Edit existing parser
            sendMessageToCSharp('edit_parser', { data, idx: editIdx });
            showNotification('Đang cập nhật parser...', 'info');
        } else {
            // Add new parser
            sendMessageToCSharp('add_parser', { data });
            showNotification('Đang thêm parser mới...', 'info');
        }
    }
    
    deleteParser(idx) {
        const parser = this.parsers[idx];
        if (!parser) return;
        
        if (confirm(`Bạn có chắc muốn xóa parser "${parser.Name}"?\n\n⚠️ Hành động này sẽ:\n❌ Xóa hoàn toàn parser khỏi cấu hình\n❌ Có thể ảnh hưởng đến việc parse log data\n❌ Không thể hoàn tác`)) {
            sendMessageToCSharp('delete_parser', { idx });
            showNotification(`Đang xóa parser "${parser.Name}"...`, 'info');
        }
    }
    
    // Function để gọi từ C#
    updateParsersFromCSharp(jsonData) {
        this.renderParsers(jsonData);
    }
}

// Initialize ParserManager
window.parserManager = new window.ParserManager();

// Expose functions globally - sử dụng tên khác để tránh conflict
window.updateParsersFromCSharpForParserModule = function(jsonData) {
    if (window.parserManager) {
        window.parserManager.renderParsers(jsonData);
    }
};

window.openEditParserPopupForParserModule = function(parserJson, idx) {
    if (window.parserManager) {
        const parser = typeof parserJson === 'string' ? JSON.parse(parserJson) : parserJson;
        window.parserManager.showEditParserPopup(idx);
    }
};

// Cũng expose function cũ để tương thích
window.updateParsersFromCSharp = function(jsonData) {
    if (window.parserManager) {
        window.parserManager.renderParsers(jsonData);
    }
};
